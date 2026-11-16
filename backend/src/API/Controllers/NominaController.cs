using FondoNet.Application.Auth;
using FondoNet.Application.Common;
using FondoNet.Application.Nomina;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FondoNet.API.Controllers;

[ApiController]
[Route("api/nomina")]
[Authorize(Roles = Roles.Admin)]
public class NominaController(INominaService nominaService) : ControllerBase
{
    [HttpPost("importar/{cajaAhorroId:int}")]
    public async Task<ActionResult<ResultadoImportacionNomina>> Importar(
        int cajaAhorroId, IFormFile? archivo, CancellationToken ct)
    {
        if (archivo is null || archivo.Length == 0)
            throw new ReglaNegocioException("Debe adjuntar un archivo .csv o .xlsx.");

        await using var stream = archivo.OpenReadStream();
        var resultado = await nominaService.ImportarAsync(cajaAhorroId, stream, archivo.FileName, ct);
        return Ok(resultado);
    }
}
