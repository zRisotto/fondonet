using FondoNet.Application.Auth;
using FondoNet.Application.Cierre;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FondoNet.API.Controllers;

[ApiController]
[Route("api/cierre")]
[Authorize(Roles = Roles.Admin)]
public class CierreController(ICierreAnualService cierreService) : ControllerBase
{
    [HttpGet("{anio:int}/estatus")]
    public async Task<ActionResult<object>> ObtenerEstatus(int anio, CancellationToken ct)
    {
        bool cerrado = await cierreService.EstaCerradoAsync(anio, ct);
        return Ok(new { anio, cerrado });
    }

    [HttpPost("{anio:int}")]
    public async Task<ActionResult<ResultadoCierreAnualDto>> Ejecutar(
        int anio, [FromQuery] bool simulacion = true, CancellationToken ct = default)
    {
        return Ok(await cierreService.EjecutarAsync(anio, simulacion, ct));
    }
}
