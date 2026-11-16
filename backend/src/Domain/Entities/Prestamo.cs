using FondoNet.Domain.Enums;

namespace FondoNet.Domain.Entities;

public class Prestamo
{
    public int Id { get; set; }
    public int AhorradorId { get; set; }
    public DateOnly FechaOtorgamiento { get; set; }
    public decimal Monto { get; set; }

    /// <summary>Tasa mensual sobre saldo insoluto (0.01 a 0.02).</summary>
    public decimal TasaInteresMensual { get; set; }

    public int PlazoQuincenas { get; set; }
    public EstatusPrestamo Estatus { get; set; } = EstatusPrestamo.Activo;
    public decimal SaldoInsoluto { get; set; }

    /// <summary>True cuando el monto excede el 80% del ahorro (ordinario + voluntario).</summary>
    public bool RequiereAval { get; set; }

    public int? AvalAhorradorId { get; set; }
    public DateOnly? FechaLiquidacion { get; set; }

    public Ahorrador? Ahorrador { get; set; }
    public Ahorrador? AvalAhorrador { get; set; }
    public ICollection<Amortizacion> Amortizaciones { get; set; } = new List<Amortizacion>();
}
