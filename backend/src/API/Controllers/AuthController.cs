using FondoNet.Application.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FondoNet.API.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var respuesta = await authService.LoginAsync(request, ct);
        if (respuesta is null)
            return Unauthorized(new { error = "Usuario o contraseña incorrectos." });

        return Ok(respuesta);
    }
}
