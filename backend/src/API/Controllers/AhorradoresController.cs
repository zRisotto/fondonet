using System.Security.Claims;
using FondoNet.Application.Ahorradores;
using FondoNet.Application.Auth;
using FondoNet.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FondoNet.API.Controllers;

[ApiController]
[Route("api/ahorradores")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Ahorrador}")]
public class AhorradoresController(IAhorradorService ahorradorService) : ControllerBase
{
    [HttpGet("cajas")]
    public async Task<ActionResult<IReadOnlyList<CajaAhorroResumenDto>>> ListarCajas(CancellationToken ct)
    {
        int? ahorradorFiltro = User.IsInRole(Roles.Admin) ? null : ObtenerAhorradorIdToken();
        if (!User.IsInRole(Roles.Admin) && !ahorradorFiltro.HasValue)
            return Forbid();

        return Ok(await ahorradorService.ListarCajasAsync(ahorradorFiltro, ct));
    }

    [HttpGet]
    [Authorize(Roles = Roles.Admin)]
    public async Task<ActionResult<IReadOnlyList<AhorradorResumenDto>>> Listar(
        [FromQuery] int? cajaAhorroId, CancellationToken ct)
    {
        return Ok(await ahorradorService.ListarAsync(cajaAhorroId, ct));
    }

    [HttpGet("avales-disponibles")]
    public async Task<ActionResult<IReadOnlyList<AvalDisponibleDto>>> ListarAvalesDisponibles(
        [FromQuery] int? ahorradorId, CancellationToken ct)
    {
        int? idSolicitante = User.IsInRole(Roles.Admin) ? ahorradorId : ObtenerAhorradorIdToken();
        if (!idSolicitante.HasValue)
            return Forbid();

        return Ok(await ahorradorService.ListarAvalesDisponiblesAsync(idSolicitante.Value, ct));
    }

    [HttpGet("{id:int}/estado-cuenta")]
    public async Task<ActionResult<EstadoCuentaDto>> ObtenerEstadoCuenta(
        int id, [FromQuery] int? anio, CancellationToken ct)
    {
        if (!PuedeAccederAhorrador(id))
            return Forbid();

        return Ok(await ahorradorService.ObtenerEstadoCuentaAsync(id, anio, ct));
    }

    private bool PuedeAccederAhorrador(int ahorradorId) =>
        User.IsInRole(Roles.Admin) || ObtenerAhorradorIdToken() == ahorradorId;

    private int? ObtenerAhorradorIdToken() =>
        int.TryParse(User.FindFirstValue(AuthService.ClaimAhorradorId), out int id) ? id : null;
}
