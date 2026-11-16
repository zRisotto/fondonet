using System.Globalization;
using System.Text;
using FondoNet.Application.Common;
using FondoNet.Application.Layouts;
using FondoNet.Domain.Enums;
using FondoNet.Domain.Finance;
using FondoNet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FondoNet.Infrastructure.Services;

public class LayoutService(FondoNetDbContext db) : ILayoutService
{
    public async Task<IReadOnlyList<IncidenciaNominaDto>> ObtenerIncidenciasNominaAsync(
        int cajaAhorroId, DateOnly quincena, CancellationToken ct = default)
    {
        var caja = await db.CajasAhorro
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cajaAhorroId, ct)
            ?? throw new NoEncontradoException($"La caja de ahorro {cajaAhorroId} no existe.");

        var ahorradores = await db.Ahorradores
            .AsNoTracking()
            .Where(a => a.CajaAhorroId == cajaAhorroId
                     && a.Estatus == EstatusAhorrador.Activo
                     && a.FechaIngreso <= quincena
                     && (a.FechaBaja == null || a.FechaBaja > quincena))
            .OrderBy(a => a.ClaveEmpleado)
            .ToListAsync(ct);

        var ids = ahorradores.Select(a => a.Id).ToList();

        var cuotasPorAhorrador = await db.Amortizaciones
            .AsNoTracking()
            .Where(am => ids.Contains(am.Prestamo!.AhorradorId)
                      && am.Prestamo.Estatus == EstatusPrestamo.Activo
                      && am.FechaVencimiento == quincena)
            .GroupBy(am => am.Prestamo!.AhorradorId)
            .Select(g => new { AhorradorId = g.Key, Cuota = g.Sum(x => x.Cuota) })
            .ToDictionaryAsync(x => x.AhorradorId, x => x.Cuota, ct);

        return ahorradores.Select(a =>
        {
            decimal ordinario = Dinero.Redondear(a.SalarioBase / 2m * caja.PorcentajeAhorro);
            decimal voluntario = a.AhorroVoluntarioQuincenal;
            decimal cuota = cuotasPorAhorrador.GetValueOrDefault(a.Id, 0m);
            decimal total = ordinario + voluntario + cuota;

            return new IncidenciaNominaDto(a.ClaveEmpleado, ordinario, voluntario, cuota, total);
        }).ToList();
    }

    public async Task<ArchivoGenerado> GenerarIncidenciasNominaAsync(
        int cajaAhorroId, DateOnly quincena, CancellationToken ct = default)
    {
        var filas = await ObtenerIncidenciasNominaAsync(cajaAhorroId, quincena, ct);

        var sb = new StringBuilder();
        sb.AppendLine("ClaveEmpleado,DescuentoAhorroOrdinario,DescuentoAhorroVoluntario,CuotaAmortizacionPrestamo,TotalDeduccion");
        foreach (var f in filas)
        {
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{f.ClaveEmpleado},{f.DescuentoAhorroOrdinario:F2},{f.DescuentoAhorroVoluntario:F2},{f.CuotaAmortizacionPrestamo:F2},{f.TotalDeduccion:F2}"));
        }

        return new ArchivoGenerado(
            $"incidencias_nomina_caja{cajaAhorroId}_{quincena:yyyyMMdd}.csv",
            "text/csv; charset=utf-8",
            Encoding.UTF8.GetBytes(sb.ToString()));
    }

    public async Task<IReadOnlyList<DispersionSpeiDto>> ObtenerDispersionSpeiAsync(
        int cajaAhorroId, int anio, CancellationToken ct = default)
    {
        var caja = await db.CajasAhorro
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == cajaAhorroId, ct)
            ?? throw new NoEncontradoException($"La caja de ahorro {cajaAhorroId} no existe.");

        DateOnly finAnio = new(anio, 12, 31);
        string conceptoCierre = $"Liquidación del ciclo {anio}";

        var retiros = await db.MovimientosAhorro
            .AsNoTracking()
            .Include(m => m.Ahorrador)
            .Where(m => m.Ahorrador!.CajaAhorroId == cajaAhorroId
                     && m.Fecha == finAnio
                     && m.Tipo == TipoMovimientoAhorro.Retiro
                     && m.Concepto == conceptoCierre)
            .OrderBy(m => m.AhorradorId)
            .ToListAsync(ct);

        return retiros.Select(m => new DispersionSpeiDto(
            caja.CuentaRetiro,
            m.Ahorrador!.Clabe,
            Math.Abs(m.Monto),
            m.Ahorrador.Nombre,
            $"LIQUIDACION AHORRO Y RENDIMIENTOS {anio}")).ToList();
    }

    public async Task<ArchivoGenerado> GenerarDispersionSpeiAsync(
        int cajaAhorroId, int anio, CancellationToken ct = default)
    {
        var filas = await ObtenerDispersionSpeiAsync(cajaAhorroId, anio, ct);

        var sb = new StringBuilder();
        sb.AppendLine("CuentaRetiro|CLABE_Destino|MontoTotal|NombreBeneficiario|Concepto");
        foreach (var f in filas)
        {
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{f.CuentaRetiro}|{f.ClabeDestino}|{f.MontoTotal:F2}|{f.NombreBeneficiario}|{f.Concepto}"));
        }

        return new ArchivoGenerado(
            $"dispersion_spei_caja{cajaAhorroId}_{anio}.txt",
            "text/plain; charset=utf-8",
            Encoding.UTF8.GetBytes(sb.ToString()));
    }
}
