namespace FondoNet.Domain.Entities;

/// <summary>Renglón de la tabla de amortización quincenal sobre saldo insoluto.</summary>
public class Amortizacion
{
    public int Id { get; set; }
    public int PrestamoId { get; set; }
    public int NumeroPago { get; set; }
    public DateOnly FechaVencimiento { get; set; }
    public decimal SaldoInicial { get; set; }
    public decimal Capital { get; set; }
    public decimal Interes { get; set; }
    public decimal Cuota { get; set; }
    public decimal SaldoFinal { get; set; }
    public bool Pagada { get; set; }

    public Prestamo? Prestamo { get; set; }
}
