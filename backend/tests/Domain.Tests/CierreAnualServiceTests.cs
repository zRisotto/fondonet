using FondoNet.Application.Common;
using FondoNet.Domain.Entities;
using FondoNet.Domain.Enums;
using FondoNet.Domain.Finance;
using FondoNet.Infrastructure.Persistence;
using FondoNet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FondoNet.Domain.Tests;

public class CierreAnualServiceTests
{
    private static FondoNetDbContext CrearDbContextConDatos2025()
    {
        var opts = new DbContextOptionsBuilder<FondoNetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new FondoNetDbContext(opts);

        db.Empresas.AddRange(
            new Empresa { Id = 1, Nombre = "Bajío", Rfc = "MBA100315AB1", Activa = true, FechaAlta = new DateOnly(2010, 3, 15) },
            new Empresa { Id = 2, Nombre = "Norte", Rfc = "SLN120820CD2", Activa = true, FechaAlta = new DateOnly(2012, 8, 20) }
        );

        db.CajasAhorro.AddRange(
            new CajaAhorro { Id = 1, EmpresaId = 1, Nombre = "Caja 1", CuentaRetiro = "012180000100200301", TasaInteresMensual = 0.015m, PorcentajeAhorro = 0.08m, Activa = true },
            new CajaAhorro { Id = 2, EmpresaId = 2, Nombre = "Caja 2", CuentaRetiro = "002580000400500602", TasaInteresMensual = 0.012m, PorcentajeAhorro = 0.10m, Activa = true }
        );

        var ahorradores = new[]
        {
            new Ahorrador { Id = 1, CajaAhorroId = 1, ClaveEmpleado = "EMP-001", Nombre = "Ana", Curp = "C1", Clabe = "CL1", Estatus = EstatusAhorrador.Activo, SalarioBase = 18000m, AhorroVoluntarioQuincenal = 500m, FechaIngreso = new DateOnly(2019, 3, 1) },
            new Ahorrador { Id = 2, CajaAhorroId = 1, ClaveEmpleado = "EMP-002", Nombre = "Carlos", Curp = "C2", Clabe = "CL2", Estatus = EstatusAhorrador.Activo, SalarioBase = 22000m, AhorroVoluntarioQuincenal = 0m, FechaIngreso = new DateOnly(2018, 8, 16) },
            new Ahorrador { Id = 3, CajaAhorroId = 1, ClaveEmpleado = "EMP-003", Nombre = "Lucía", Curp = "C3", Clabe = "CL3", Estatus = EstatusAhorrador.Activo, SalarioBase = 15000m, AhorroVoluntarioQuincenal = 300m, FechaIngreso = new DateOnly(2025, 4, 1) },
            new Ahorrador { Id = 4, CajaAhorroId = 2, ClaveEmpleado = "EMP-101", Nombre = "Jorge", Curp = "C4", Clabe = "CL4", Estatus = EstatusAhorrador.Activo, SalarioBase = 25000m, AhorroVoluntarioQuincenal = 1000m, FechaIngreso = new DateOnly(2017, 1, 9) },
            new Ahorrador { Id = 5, CajaAhorroId = 2, ClaveEmpleado = "EMP-102", Nombre = "Sofía", Curp = "C5", Clabe = "CL5", Estatus = EstatusAhorrador.Activo, SalarioBase = 16000m, AhorroVoluntarioQuincenal = 0m, FechaIngreso = new DateOnly(2020, 10, 1) },
            new Ahorrador { Id = 6, CajaAhorroId = 2, ClaveEmpleado = "EMP-103", Nombre = "Ricardo", Curp = "C6", Clabe = "CL6", Estatus = EstatusAhorrador.Inactivo, SalarioBase = 14000m, AhorroVoluntarioQuincenal = 200m, FechaIngreso = new DateOnly(2021, 5, 17), FechaBaja = new DateOnly(2026, 6, 30) }
        };
        db.Ahorradores.AddRange(ahorradores);

        // Movimientos quincenales 2025 idénticos al seed
        for (int mes = 1; mes <= 12; mes++)
        {
            foreach (var fecha in new[] { new DateOnly(2025, mes, 15), new DateOnly(2025, mes, DateTime.DaysInMonth(2025, mes)) })
            {
                foreach (var a in ahorradores.Where(a => fecha >= a.FechaIngreso))
                {
                    decimal pct = a.CajaAhorroId == 1 ? 0.08m : 0.10m;
                    decimal ord = Dinero.Redondear(a.SalarioBase / 2m * pct);
                    db.MovimientosAhorro.Add(new MovimientoAhorro { AhorradorId = a.Id, Fecha = fecha, Tipo = TipoMovimientoAhorro.Ordinario, Monto = ord, Concepto = "Ord" });
                    db.MovimientosAhorro.Add(new MovimientoAhorro { AhorradorId = a.Id, Fecha = fecha, Tipo = TipoMovimientoAhorro.Patronal, Monto = ord, Concepto = "Pat" });
                    if (a.AhorroVoluntarioQuincenal > 0)
                        db.MovimientosAhorro.Add(new MovimientoAhorro { AhorradorId = a.Id, Fecha = fecha, Tipo = TipoMovimientoAhorro.Voluntario, Monto = a.AhorroVoluntarioQuincenal, Concepto = "Vol" });
                }
            }
        }

        // Préstamos 2025 de Ana (id=1, Caja 1) y Sofía (id=5, Caja 2) pagados en 2025
        var pAna = new Prestamo
        {
            Id = 1, AhorradorId = 1, FechaOtorgamiento = new DateOnly(2025, 6, 30),
            Monto = 10000m, TasaInteresMensual = 0.015m, PlazoQuincenas = 12,
            Estatus = EstatusPrestamo.Liquidado, SaldoInsoluto = 0m,
            Amortizaciones = AmortizacionCalculator.Generar(10000m, 0.015m, 12, new DateOnly(2025, 6, 30))
                .Select(r => new Amortizacion { NumeroPago = r.NumeroPago, FechaVencimiento = r.FechaVencimiento, SaldoInicial = r.SaldoInicial, Capital = r.Capital, Interes = r.Interes, Cuota = r.Cuota, SaldoFinal = r.SaldoFinal, Pagada = true })
                .ToList()
        };
        var pSofia = new Prestamo
        {
            Id = 2, AhorradorId = 5, FechaOtorgamiento = new DateOnly(2025, 5, 15),
            Monto = 5000m, TasaInteresMensual = 0.012m, PlazoQuincenas = 12,
            Estatus = EstatusPrestamo.Liquidado, SaldoInsoluto = 0m,
            Amortizaciones = AmortizacionCalculator.Generar(5000m, 0.012m, 12, new DateOnly(2025, 5, 15))
                .Select(r => new Amortizacion { NumeroPago = r.NumeroPago, FechaVencimiento = r.FechaVencimiento, SaldoInicial = r.SaldoInicial, Capital = r.Capital, Interes = r.Interes, Cuota = r.Cuota, SaldoFinal = r.SaldoFinal, Pagada = true })
                .ToList()
        };
        db.Prestamos.AddRange(pAna, pSofia);

        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Simulacion_2025_da_691_31_y_no_persiste_movimientos()
    {
        using var db = CrearDbContextConDatos2025();
        var service = new CierreAnualService(db);

        var res = await service.EjecutarAsync(2025, simulacion: true);

        Assert.True(res.Simulacion);
        Assert.False(res.YaCerrado);
        Assert.Equal(691.31m, res.TotalBolsa);
        Assert.Equal(691.31m, res.TotalRendimientos);
        Assert.Equal(new[] { 211.40m, 191.79m, 90.99m }, res.Cajas[0].Ahorradores.Select(a => a.Rendimiento));
        Assert.Equal(new[] { 102.98m, 47.08m, 47.07m }, res.Cajas[1].Ahorradores.Select(a => a.Rendimiento));

        // No escribió RENDIMIENTO, RETIRO ni CierreAnual
        Assert.False(await db.MovimientosAhorro.AnyAsync(m => m.Tipo == TipoMovimientoAhorro.Rendimiento));
        Assert.False(await db.MovimientosAhorro.AnyAsync(m => m.Tipo == TipoMovimientoAhorro.Retiro));
        Assert.False(await service.EstaCerradoAsync(2025));
    }

    [Fact]
    public async Task Cierre_real_genera_rendimiento_y_retiro_dejando_saldo_en_cero_y_es_idempotente()
    {
        using var db = CrearDbContextConDatos2025();
        var service = new CierreAnualService(db);

        var primero = await service.EjecutarAsync(2025, simulacion: false);
        Assert.False(primero.Simulacion);
        Assert.False(primero.YaCerrado);
        Assert.Equal(691.31m, primero.TotalRendimientos);
        Assert.True(await service.EstaCerradoAsync(2025));

        // Cada ahorrador tiene su RENDIMIENTO y su RETIRO al 31/dic/2025 y su saldo queda exactamente en 0.00
        Assert.Equal(6, await db.MovimientosAhorro.CountAsync(m => m.Tipo == TipoMovimientoAhorro.Rendimiento));
        Assert.Equal(6, await db.MovimientosAhorro.CountAsync(m => m.Tipo == TipoMovimientoAhorro.Retiro));

        for (int ahorradorId = 1; ahorradorId <= 6; ahorradorId++)
        {
            decimal saldoAlCierre = await db.MovimientosAhorro
                .Where(m => m.AhorradorId == ahorradorId && m.Fecha <= new DateOnly(2025, 12, 31))
                .SumAsync(m => m.Monto);
            Assert.Equal(0.00m, saldoAlCierre);
        }

        int totalMovimientosTrasPrimero = await db.MovimientosAhorro.CountAsync();

        // Segunda corrida real: devuelve YaCerrado = true y NO duplica movimientos ni cierres
        var segundo = await service.EjecutarAsync(2025, simulacion: false);
        Assert.True(segundo.YaCerrado);
        Assert.Equal(691.31m, segundo.TotalRendimientos);
        Assert.Equal(totalMovimientosTrasPrimero, await db.MovimientosAhorro.CountAsync());
        Assert.Equal(2, await db.CierresAnuales.CountAsync(c => c.Anio == 2025));
    }

    [Fact]
    public async Task Cierre_real_de_anio_en_curso_es_rechazado_pero_simulacion_si_permite()
    {
        using var db = CrearDbContextConDatos2025();
        var service = new CierreAnualService(db);
        int anioEnCurso = DateTime.Today.Year;

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.EjecutarAsync(anioEnCurso, simulacion: false));

        var sim = await service.EjecutarAsync(anioEnCurso, simulacion: true);
        Assert.True(sim.Simulacion);
    }
}
