namespace FondoNet.Application.Auth;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Ahorrador = "Ahorrador";
}

public sealed record LoginRequest(string Usuario, string Password);

public sealed record LoginResponse(string Token, DateTime ExpiraUtc, string Usuario, string Rol, int? AhorradorId);

public interface IAuthService
{
    /// <summary>Devuelve null si usuario/contraseña no son válidos.</summary>
    Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
}
