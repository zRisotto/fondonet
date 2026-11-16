using System.Globalization;
using ClosedXML.Excel;
using FondoNet.Application.Common;
using FondoNet.Application.Nomina;
using FondoNet.Domain.Finance;

namespace FondoNet.Infrastructure.Services;

public class NominaParser : INominaParser
{
    public ResultadoParseoNomina Parsear(Stream archivo, string nombreArchivo)
    {
        string ext = Path.GetExtension(nombreArchivo).ToLowerInvariant();
        return ext switch
        {
            ".csv" => ParsearCsv(archivo),
            ".xlsx" => ParsearXlsx(archivo),
            _ => throw new ReglaNegocioException("Formato no soportado. Use .csv o .xlsx.")
        };
    }

    private static ResultadoParseoNomina ParsearCsv(Stream stream)
    {
        var registros = new List<RegistroNomina>();
        var errores = new List<ErrorRenglon>();

        using var reader = new StreamReader(stream, leaveOpen: true);
        int renglon = 0;
        string? linea;
        while ((linea = reader.ReadLine()) is not null)
        {
            renglon++;
            if (string.IsNullOrWhiteSpace(linea))
                continue;

            var cols = linea.Split(',').Select(c => c.Trim().Trim('"')).ToArray();
            if (renglon == 1 && EsEncabezado(cols))
                continue;

            ProcesarRenglon(renglon, cols, registros, errores);
        }

        return new ResultadoParseoNomina(registros, errores);
    }

    private static ResultadoParseoNomina ParsearXlsx(Stream stream)
    {
        var registros = new List<RegistroNomina>();
        var errores = new List<ErrorRenglon>();

        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var filas = sheet.RowsUsed().ToList();

        foreach (var fila in filas)
        {
            int renglon = fila.RowNumber();
            var cols = new[]
            {
                fila.Cell(1).GetString().Trim(),
                LeerFechaCelda(fila.Cell(2)),
                fila.Cell(3).GetString().Trim(),
                fila.Cell(4).GetString().Trim()
            };

            if (cols.All(string.IsNullOrWhiteSpace))
                continue;

            if (renglon == 1 && EsEncabezado(cols))
                continue;

            ProcesarRenglon(renglon, cols, registros, errores);
        }

        return new ResultadoParseoNomina(registros, errores);
    }

    private static string LeerFechaCelda(IXLCell celda) =>
        celda.DataType == XLDataType.DateTime
            ? DateOnly.FromDateTime(celda.GetDateTime()).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
            : celda.GetString().Trim();

    private static bool EsEncabezado(string[] cols) =>
        cols.Length > 0 && cols[0].Replace(" ", "").Equals("ClaveEmpleado", StringComparison.OrdinalIgnoreCase);

    private static void ProcesarRenglon(
        int renglon, string[] cols, List<RegistroNomina> registros, List<ErrorRenglon> errores)
    {
        if (cols.Length < 4)
        {
            errores.Add(new ErrorRenglon(renglon, "Se esperan 4 columnas: ClaveEmpleado, Fecha, TipoMovimiento, SalarioBase."));
            return;
        }

        string clave = cols[0];
        if (string.IsNullOrWhiteSpace(clave))
        {
            errores.Add(new ErrorRenglon(renglon, "ClaveEmpleado vacía."));
            return;
        }

        if (!DateOnly.TryParse(cols[1], CultureInfo.InvariantCulture, DateTimeStyles.None, out var fecha))
        {
            errores.Add(new ErrorRenglon(renglon, $"Fecha inválida: '{cols[1]}'."));
            return;
        }

        if (!Enum.TryParse<TipoMovimientoNomina>(cols[2], ignoreCase: true, out var tipo))
        {
            errores.Add(new ErrorRenglon(renglon, $"TipoMovimiento inválido: '{cols[2]}' (use ALTA, BAJA, INCAPACIDAD o LICENCIA)."));
            return;
        }

        if (!decimal.TryParse(cols[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var salario) || salario <= 0)
        {
            errores.Add(new ErrorRenglon(renglon, $"SalarioBase inválido: '{cols[3]}'."));
            return;
        }

        registros.Add(new RegistroNomina(renglon, clave, fecha, tipo, Dinero.Redondear(salario)));
    }
}
