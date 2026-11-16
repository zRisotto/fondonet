using FondoNet.Application.Auth;
using FondoNet.Application.Layouts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FondoNet.API.Controllers;

[ApiController]
[Route("api/layouts")]
[Authorize(Roles = Roles.Admin)]
public class LayoutsController(ILayoutService layoutService) : ControllerBase
{
    [HttpGet("nomina/{cajaAhorroId:int}")]
    public async Task<ActionResult<IReadOnlyList<IncidenciaNominaDto>>> VerIncidenciasNomina(
        int cajaAhorroId, [FromQuery] DateOnly quincena, CancellationToken ct)
    {
        return Ok(await layoutService.ObtenerIncidenciasNominaAsync(cajaAhorroId, quincena, ct));
    }

    [HttpGet("nomina/{cajaAhorroId:int}/archivo")]
    public async Task<IActionResult> DescargarIncidenciasNomina(
        int cajaAhorroId, [FromQuery] DateOnly quincena, CancellationToken ct)
    {
        var archivo = await layoutService.GenerarIncidenciasNominaAsync(cajaAhorroId, quincena, ct);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }

    [HttpGet("spei/{cajaAhorroId:int}/{anio:int}")]
    public async Task<ActionResult<IReadOnlyList<DispersionSpeiDto>>> VerDispersionSpei(
        int cajaAhorroId, int anio, CancellationToken ct)
    {
        return Ok(await layoutService.ObtenerDispersionSpeiAsync(cajaAhorroId, anio, ct));
    }

    [HttpGet("spei/{cajaAhorroId:int}/{anio:int}/archivo")]
    public async Task<IActionResult> DescargarDispersionSpei(
        int cajaAhorroId, int anio, CancellationToken ct)
    {
        var archivo = await layoutService.GenerarDispersionSpeiAsync(cajaAhorroId, anio, ct);
        return File(archivo.Contenido, archivo.ContentType, archivo.NombreArchivo);
    }
}
