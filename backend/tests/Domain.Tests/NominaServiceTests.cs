using System.Text;
using FondoNet.Domain.Entities;
using FondoNet.Domain.Enums;
using FondoNet.Infrastructure.Persistence;
using FondoNet.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace FondoNet.Domain.Tests;

public class NominaServiceTests
{
    private static FondoNetDbContext CrearDbContext()
    {
        var opts = new DbContextOptionsBuilder<FondoNetDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var db = new FondoNetDbContext(opts);

        db.Empresas.Add(new Empresa { Id = 1, Nombre = "Empresa 1", Rfc = "EMP010101AAA", Activa = true, FechaAlta = new DateOnly(2020, 1, 1) });
        db.CajasAhorro.Add(new CajaAhorro { Id = 1, EmpresaId = 1, Nombre = "Caja 1", CuentaRetiro = "012180000100200301", TasaInteresMensual = 0.015m, PorcentajeAhorro = 0.08m, Activa = true });

        db.Ahorradores.AddRange(
            new Ahorrador { Id = 1, CajaAhorroId = 1, ClaveEmpleado = "EMP-001", Nombre = "Ana", Curp = "CURP00000000000001", Clabe = "012180001234567891", Estatus = EstatusAhorrador.Activo, SalarioBase = 18000m, FechaIngreso = new DateOnly(2020, 1, 1) },
            new Ahorrador { Id = 2, CajaAhorroId = 1, ClaveEmpleado = "EMP-002", Nombre = "Carlos", Curp = "CURP00000000000002", Clabe = "012180001234567892", Estatus = EstatusAhorrador.Licencia, SalarioBase = 20000m, FechaIngreso = new DateOnly(2020, 1, 1) }
        );

        // Saldo acumulado de Ana = 3,500.00
        db.MovimientosAhorro.AddRange(
            new MovimientoAhorro { Id = 1, AhorradorId = 1, Fecha = new DateOnly(2026, 1, 15), Tipo = TipoMovimientoAhorro.Ordinario, Monto = 1500m, Concepto = "Ord" },
            new MovimientoAhorro { Id = 2, AhorradorId = 1, Fecha = new DateOnly(2026, 1, 15), Tipo = TipoMovimientoAhorro.Patronal, Monto = 1500m, Concepto = "Pat" },
            new MovimientoAhorro { Id = 3, AhorradorId = 1, Fecha = new DateOnly(2026, 1, 15), Tipo = TipoMovimientoAhorro.Voluntario, Monto = 500m, Concepto = "Vol" }
        );

        db.SaveChanges();
        return db;
    }

    private static MemoryStream CsvStream(string contenido) =>
        new(Encoding.UTF8.GetBytes(contenido));

    [Fact]
    public async Task Renglon_invalido_y_ALTA_inexistente_se_reportan_sin_abortar_el_resto()
    {
        using var db = CrearDbContext();
        var service = new NominaService(db, new NominaParser());

        string csv = string.Join("\n",
            "ClaveEmpleado,Fecha,TipoMovimiento,SalarioBase",
            "EMP-001,fecha-mala,LICENCIA,18000.00",          // renglón 2 inválido (fecha)
            "EMP-999,2026-06-15,ALTA,19000.00",              // renglón 3 clave inexistente
            "EMP-002,2026-06-15,ALTA,22500.00"               // renglón 4 válido: reactiva a Carlos
        );

        var res = await service.ImportarAsync(1, CsvStream(csv), "nomina.csv");

        Assert.Equal(3, res.RenglonesLeidos);
        Assert.Equal(1, res.RenglonesAplicados);
        Assert.Equal(2, res.Errores.Count);
        Assert.Contains(res.Errores, e => e.Renglon == 2);
        Assert.Contains(res.Errores, e => e.Renglon == 3 && e.Mensaje.Contains("EMP-999"));

        var carlos = await db.Ahorradores.FindAsync(2);
        Assert.Equal(EstatusAhorrador.Activo, carlos!.Estatus);
        Assert.Equal(22500.00m, carlos.SalarioBase);
    }

    [Fact]
    public async Task Baja_con_saldo_genera_retiro_y_repetir_el_archivo_no_duplica_retiros()
    {
        using var db = CrearDbContext();
        var service = new NominaService(db, new NominaParser());

        string csv = string.Join("\n",
            "ClaveEmpleado,Fecha,TipoMovimiento,SalarioBase",
            "EMP-001,2026-06-30,BAJA,18000.00"
        );

        // Primera importación: marca INACTIVO, asigna FechaBaja y liquida saldo (-3,500.00)
        var r1 = await service.ImportarAsync(1, CsvStream(csv), "baja.csv");
        Assert.Equal(1, r1.RenglonesAplicados);
        Assert.Empty(r1.Errores);

        var ana = await db.Ahorradores.FindAsync(1);
        Assert.Equal(EstatusAhorrador.Inactivo, ana!.Estatus);
        Assert.Equal(new DateOnly(2026, 6, 30), ana.FechaBaja);

        var retiros1 = await db.MovimientosAhorro
            .Where(m => m.AhorradorId == 1 && m.Tipo == TipoMovimientoAhorro.Retiro)
            .ToListAsync();
        Assert.Single(retiros1);
        Assert.Equal(-3500.00m, retiros1[0].Monto);
        Assert.Equal(0.00m, await db.MovimientosAhorro.Where(m => m.AhorradorId == 1).SumAsync(m => m.Monto));

        // Segunda importación con el mismo archivo: el saldo ya es 0, por lo que NO genera otro RETIRO
        var r2 = await service.ImportarAsync(1, CsvStream(csv), "baja.csv");
        Assert.Equal(1, r2.RenglonesAplicados);

        var retiros2 = await db.MovimientosAhorro
            .Where(m => m.AhorradorId == 1 && m.Tipo == TipoMovimientoAhorro.Retiro)
            .ToListAsync();
        Assert.Single(retiros2);
    }
}
