using FondoNet.Application.Common;
using FondoNet.Application.Prestamos;
using FondoNet.Domain.Entities;
using FondoNet.Domain.Enums;
using FondoNet.Infrastructure.Persistence;
using FondoNet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FondoNet.Domain.Tests;

public class PrestamoServiceTests
{
    private static readonly DateOnly FechaSolicitud = new(2026, 5, 15);

    /// <summary>
    /// Crea un DbContext en memoria con:
    /// - Ahorrador 1 (activo, Caja 1): en 2026 tiene ORDINARIO 6,000 + VOLUNTARIO 4,000 = 10,000 -> tope 80% = 8,000.00.
    ///   También tiene PATRONAL 6,000 en 2026 y ORDINARIO 20,000 en 2025 (para verificar que NO cuenten en el tope).
    /// - Ahorrador 2 (activo, Caja 1): aval válido.
    /// - Ahorrador 3 (inactivo, Caja 1): aval inválido por estatus.
    /// - Ahorrador 4 (activo, Caja 2): aval inválido por ser de otra caja.
    /// </summary>
    private static FondoNetDbContext CrearDbContext()
    {
        var opts = new DbContextOptionsBuilder<FondoNetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new FondoNetDbContext(opts);

        db.Empresas.Add(new Empresa { Id = 1, Nombre = "Empresa 1", Rfc = "EMP010101AAA", Activa = true, FechaAlta = new DateOnly(2020, 1, 1) });
        db.CajasAhorro.AddRange(
            new CajaAhorro { Id = 1, EmpresaId = 1, Nombre = "Caja 1", CuentaRetiro = "012180000100200301", TasaInteresMensual = 0.015m, PorcentajeAhorro = 0.08m, Activa = true },
            new CajaAhorro { Id = 2, EmpresaId = 1, Nombre = "Caja 2", CuentaRetiro = "002580000400500602", TasaInteresMensual = 0.012m, PorcentajeAhorro = 0.10m, Activa = true }
        );

        db.Ahorradores.AddRange(
            new Ahorrador { Id = 1, CajaAhorroId = 1, ClaveEmpleado = "E1", Nombre = "Solicitante", Curp = "CURP00000000000001", Clabe = "012180001234567891", Estatus = EstatusAhorrador.Activo, SalarioBase = 18000m, FechaIngreso = new DateOnly(2024, 1, 1) },
            new Ahorrador { Id = 2, CajaAhorroId = 1, ClaveEmpleado = "E2", Nombre = "Aval Activo", Curp = "CURP00000000000002", Clabe = "012180001234567892", Estatus = EstatusAhorrador.Activo, SalarioBase = 20000m, FechaIngreso = new DateOnly(2024, 1, 1) },
            new Ahorrador { Id = 3, CajaAhorroId = 1, ClaveEmpleado = "E3", Nombre = "Aval Inactivo", Curp = "CURP00000000000003", Clabe = "012180001234567893", Estatus = EstatusAhorrador.Inactivo, SalarioBase = 15000m, FechaIngreso = new DateOnly(2024, 1, 1), FechaBaja = new DateOnly(2026, 3, 1) },
            new Ahorrador { Id = 4, CajaAhorroId = 2, ClaveEmpleado = "E4", Nombre = "Aval Otra Caja", Curp = "CURP00000000000004", Clabe = "012180001234567894", Estatus = EstatusAhorrador.Activo, SalarioBase = 22000m, FechaIngreso = new DateOnly(2024, 1, 1) }
        );

        db.MovimientosAhorro.AddRange(
            // Ciclo anterior 2025 (no debe contar en el tope 2026)
            new MovimientoAhorro { Id = 1, AhorradorId = 1, Fecha = new DateOnly(2025, 6, 15), Tipo = TipoMovimientoAhorro.Ordinario, Monto = 20000m, Concepto = "2025" },
            // Ciclo vigente 2026: ORDINARIO 6,000 + VOLUNTARIO 4,000 = 10,000 (tope 80% = 8,000)
            new MovimientoAhorro { Id = 2, AhorradorId = 1, Fecha = new DateOnly(2026, 3, 15), Tipo = TipoMovimientoAhorro.Ordinario, Monto = 6000m, Concepto = "Ord" },
            new MovimientoAhorro { Id = 3, AhorradorId = 1, Fecha = new DateOnly(2026, 3, 15), Tipo = TipoMovimientoAhorro.Voluntario, Monto = 4000m, Concepto = "Vol" },
            // PATRONAL en 2026 (no debe contar en el tope estricto)
            new MovimientoAhorro { Id = 4, AhorradorId = 1, Fecha = new DateOnly(2026, 3, 15), Tipo = TipoMovimientoAhorro.Patronal, Monto = 6000m, Concepto = "Pat" },
            // Movimiento posterior a la fecha de solicitud (no debe contar)
            new MovimientoAhorro { Id = 5, AhorradorId = 1, Fecha = new DateOnly(2026, 6, 15), Tipo = TipoMovimientoAhorro.Ordinario, Monto = 5000m, Concepto = "Futuro" }
        );

        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task Tope_exacto_al_80_por_ciento_es_valido_sin_aval()
    {
        using var db = CrearDbContext();
        var service = new PrestamoService(db);

        var res = await service.SolicitarAsync(new SolicitudPrestamoRequest(
            AhorradorId: 1, Monto: 8000.00m, PlazoQuincenas: 12,
            TasaInteresMensual: null, FechaSolicitud: FechaSolicitud, AvalAhorradorId: null));

        Assert.False(res.Prestamo.RequiereAval);
        Assert.Null(res.Prestamo.AvalAhorradorId);
        Assert.Equal(8000.00m, res.Prestamo.Monto);
        Assert.Equal(12, res.Amortizaciones.Count);
    }

    [Fact]
    public async Task Un_centavo_arriba_del_80_por_ciento_sin_aval_es_rechazado()
    {
        using var db = CrearDbContext();
        var service = new PrestamoService(db);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.SolicitarAsync(new SolicitudPrestamoRequest(
                AhorradorId: 1, Monto: 8000.01m, PlazoQuincenas: 12,
                TasaInteresMensual: null, FechaSolicitud: FechaSolicitud, AvalAhorradorId: null)));
    }

    [Fact]
    public async Task Un_centavo_arriba_del_80_por_ciento_con_aval_valido_es_aceptado()
    {
        using var db = CrearDbContext();
        var service = new PrestamoService(db);

        var res = await service.SolicitarAsync(new SolicitudPrestamoRequest(
            AhorradorId: 1, Monto: 8000.01m, PlazoQuincenas: 12,
            TasaInteresMensual: null, FechaSolicitud: FechaSolicitud, AvalAhorradorId: 2));

        Assert.True(res.Prestamo.RequiereAval);
        Assert.Equal(2, res.Prestamo.AvalAhorradorId);
    }

    [Fact]
    public async Task Rechaza_aval_inexistente()
    {
        using var db = CrearDbContext();
        var service = new PrestamoService(db);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.SolicitarAsync(new SolicitudPrestamoRequest(
                AhorradorId: 1, Monto: 8000.01m, PlazoQuincenas: 12,
                TasaInteresMensual: null, FechaSolicitud: FechaSolicitud, AvalAhorradorId: 999)));
    }

    [Fact]
    public async Task Rechaza_aval_inactivo()
    {
        using var db = CrearDbContext();
        var service = new PrestamoService(db);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.SolicitarAsync(new SolicitudPrestamoRequest(
                AhorradorId: 1, Monto: 8000.01m, PlazoQuincenas: 12,
                TasaInteresMensual: null, FechaSolicitud: FechaSolicitud, AvalAhorradorId: 3)));
    }

    [Fact]
    public async Task Rechaza_aval_de_otra_caja()
    {
        using var db = CrearDbContext();
        var service = new PrestamoService(db);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.SolicitarAsync(new SolicitudPrestamoRequest(
                AhorradorId: 1, Monto: 8000.01m, PlazoQuincenas: 12,
                TasaInteresMensual: null, FechaSolicitud: FechaSolicitud, AvalAhorradorId: 4)));
    }

    [Fact]
    public async Task Rechaza_aval_igual_al_solicitante()
    {
        using var db = CrearDbContext();
        var service = new PrestamoService(db);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            service.SolicitarAsync(new SolicitudPrestamoRequest(
                AhorradorId: 1, Monto: 8000.01m, PlazoQuincenas: 12,
                TasaInteresMensual: null, FechaSolicitud: FechaSolicitud, AvalAhorradorId: 1)));
    }
}
