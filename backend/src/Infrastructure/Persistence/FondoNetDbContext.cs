using FondoNet.Domain.Entities;
using FondoNet.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FondoNet.Infrastructure.Persistence;

/// <summary>
/// El esquema lo crea db/01_schema.sql (initdb de MariaDB); EF Core solo lo mapea.
/// Tablas y columnas en snake_case minúsculas.
/// </summary>
public class FondoNetDbContext(DbContextOptions<FondoNetDbContext> options) : DbContext(options)
{
    public DbSet<Empresa> Empresas => Set<Empresa>();
    public DbSet<CajaAhorro> CajasAhorro => Set<CajaAhorro>();
    public DbSet<Ahorrador> Ahorradores => Set<Ahorrador>();
    public DbSet<MovimientoAhorro> MovimientosAhorro => Set<MovimientoAhorro>();
    public DbSet<Prestamo> Prestamos => Set<Prestamo>();
    public DbSet<Amortizacion> Amortizaciones => Set<Amortizacion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Empresa>(e =>
        {
            e.ToTable("empresas");
            e.HasIndex(x => x.Rfc).IsUnique();
        });

        modelBuilder.Entity<CajaAhorro>(e =>
        {
            e.ToTable("cajas_ahorro");
            e.Property(x => x.TasaInteresMensual).HasPrecision(5, 4);
            e.Property(x => x.PorcentajeAhorro).HasPrecision(5, 4);
            e.HasOne(x => x.Empresa).WithMany(x => x.CajasAhorro).HasForeignKey(x => x.EmpresaId);
        });

        modelBuilder.Entity<Ahorrador>(e =>
        {
            e.ToTable("ahorradores");
            e.Property(x => x.Estatus).HasConversion(EnumMayusculas<EstatusAhorrador>());
            e.HasIndex(x => x.Curp).IsUnique();
            e.HasIndex(x => new { x.CajaAhorroId, x.ClaveEmpleado }).IsUnique();
            e.HasOne(x => x.CajaAhorro).WithMany(x => x.Ahorradores).HasForeignKey(x => x.CajaAhorroId);
        });

        modelBuilder.Entity<MovimientoAhorro>(e =>
        {
            e.ToTable("movimientos_ahorro");
            e.Property(x => x.Tipo).HasConversion(EnumMayusculas<TipoMovimientoAhorro>());
            e.HasIndex(x => new { x.AhorradorId, x.Fecha });
            e.HasOne(x => x.Ahorrador).WithMany(x => x.Movimientos).HasForeignKey(x => x.AhorradorId);
        });

        modelBuilder.Entity<Prestamo>(e =>
        {
            e.ToTable("prestamos");
            e.Property(x => x.TasaInteresMensual).HasPrecision(5, 4);
            e.Property(x => x.Estatus).HasConversion(EnumMayusculas<EstatusPrestamo>());
            e.HasOne(x => x.Ahorrador).WithMany(x => x.Prestamos).HasForeignKey(x => x.AhorradorId);
            e.HasOne(x => x.AvalAhorrador).WithMany().HasForeignKey(x => x.AvalAhorradorId);
        });

        modelBuilder.Entity<Amortizacion>(e =>
        {
            e.ToTable("amortizaciones");
            e.HasIndex(x => new { x.PrestamoId, x.NumeroPago }).IsUnique();
            e.HasOne(x => x.Prestamo).WithMany(x => x.Amortizaciones).HasForeignKey(x => x.PrestamoId);
        });

        // Convenciones globales: columnas snake_case y dinero DECIMAL(18,2).
        foreach (var entidad in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var propiedad in entidad.GetProperties())
            {
                propiedad.SetColumnName(ASnakeCase(propiedad.Name));

                if (propiedad.ClrType == typeof(decimal) && propiedad.GetPrecision() is null)
                {
                    propiedad.SetPrecision(18);
                    propiedad.SetScale(2);
                }
            }
        }
    }

    /// <summary>Guarda el enum como texto en mayúsculas (ACTIVO, RENDIMIENTO, ...).</summary>
    private static ValueConverterMayusculas<T> EnumMayusculas<T>() where T : struct, Enum => new();

    private static string ASnakeCase(string nombre) =>
        string.Concat(nombre.Select((c, i) =>
            i > 0 && char.IsUpper(c) ? "_" + char.ToLowerInvariant(c) : char.ToLowerInvariant(c).ToString()));

    private sealed class ValueConverterMayusculas<T>()
        : Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<T, string>(
            v => v.ToString().ToUpperInvariant(),
            v => Enum.Parse<T>(v, true))
        where T : struct, Enum;
}
