using System.Security.Claims;
using FondoNet.Application.Auth;
using FondoNet.Application.Prestamos;
using FondoNet.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FondoNet.API.Controllers;

[ApiController]
[Route("api/prestamos")]
[Authorize(Roles = $"{Roles.Admin},{Roles.Ahorrador}")]
public class PrestamosController(IPrestamoService prestamoService) : ControllerBase
{
    [HttpGet("capacidad/{ahorradorId:int}")]
    public async Task<ActionResult<CapacidadCreditoDto>> ObtenerCapacidad(
        int ahorradorId, [FromQuery] DateOnly? fecha, CancellationToken ct)
    {
        if (!PuedeAccederAhorrador(ahorradorId))
            return Forbid();

        return Ok(await prestamoService.ObtenerCapacidadAsync(ahorradorId, fecha, ct));
    }

    [HttpPost("simular")]
    public async Task<ActionResult<SimulacionPrestamoDto>> Simular(
        [FromBody] SolicitudPrestamoRequest solicitud, CancellationToken ct)
    {
        if (!PuedeAccederAhorrador(solicitud.AhorradorId))
            return Forbid();

        return Ok(await prestamoService.SimularAsync(solicitud, ct));
    }

    [HttpPost]
    public async Task<ActionResult<PrestamoDetalleDto>> Solicitar(
        [FromBody] SolicitudPrestamoRequest solicitud, CancellationToken ct)
    {
        if (!PuedeAccederAhorrador(solicitud.AhorradorId))
            return Forbid();

        var creado = await prestamoService.SolicitarAsync(solicitud, ct);
        return CreatedAtAction(nameof(Obtener), new { id = creado.Prestamo.Id }, creado);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<PrestamoDetalleDto>> Obtener(int id, CancellationToken ct)
    {
        var detalle = await prestamoService.ObtenerAsync(id, ct);
        if (!PuedeAccederAhorrador(detalle.Prestamo.AhorradorId))
            return Forbid();

        return Ok(detalle);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PrestamoDto>>> Listar(
        [FromQuery] int? ahorradorId, [FromQuery] int? cajaAhorroId, CancellationToken ct)
    {
        int? ahorradorFiltro = User.IsInRole(Roles.Admin) ? ahorradorId : ObtenerAhorradorIdToken();
        return Ok(await prestamoService.ListarAsync(ahorradorFiltro, cajaAhorroId, ct));
    }

    private bool PuedeAccederAhorrador(int ahorradorId) =>
        User.IsInRole(Roles.Admin) || ObtenerAhorradorIdToken() == ahorradorId;

    private int? ObtenerAhorradorIdToken() =>
        int.TryParse(User.FindFirstValue(AuthService.ClaimAhorradorId), out int id) ? id : null;
}
