using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FondoNet.Application.Auth;
using FondoNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace FondoNet.Infrastructure.Services;

public class AuthService(FondoNetDbContext db, IConfiguration config) : IAuthService
{
    public const string ClaimAhorradorId = "ahorrador_id";

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Usuario) || string.IsNullOrWhiteSpace(request.Password))
            return null;

        var usuario = await db.Usuarios
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.NombreUsuario == request.Usuario && u.Activo, ct);

        if (usuario is null || !VerificarPassword(request.Password, usuario.PasswordHash))
            return null;

        string rol = usuario.Rol.Equals("ADMIN", StringComparison.OrdinalIgnoreCase)
            ? Roles.Admin
            : Roles.Ahorrador;

        string key = config["Jwt:Key"] ?? "FondoNet_Clave_Secreta_Desarrollo_2026_Min32Bytes!";
        string issuer = config["Jwt:Issuer"] ?? "FondoNet";
        string audience = config["Jwt:Audience"] ?? "FondoNetClient";

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new(ClaimTypes.Role, rol)
        };

        if (usuario.AhorradorId.HasValue)
            claims.Add(new Claim(ClaimAhorradorId, usuario.AhorradorId.Value.ToString()));

        var expira = DateTime.UtcNow.AddHours(8);
        var creds = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
            SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expira,
            signingCredentials: creds);

        string jwt = new JwtSecurityTokenHandler().WriteToken(token);
        return new LoginResponse(jwt, expira, usuario.NombreUsuario, rol, usuario.AhorradorId);
    }

    public static bool VerificarPassword(string password, string formatoHash)
    {
        var partes = formatoHash.Split('$');
        if (partes.Length != 3 || !int.TryParse(partes[0], out int iteraciones))
            return false;

        byte[] salt = Convert.FromBase64String(partes[1]);
        byte[] esperado = Convert.FromBase64String(partes[2]);
        byte[] calculado = Rfc2898DeriveBytes.Pbkdf2(
            Encoding.UTF8.GetBytes(password),
            salt,
            iteraciones,
            HashAlgorithmName.SHA256,
            esperado.Length);

        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}
