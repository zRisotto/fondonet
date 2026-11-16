namespace FondoNet.Domain.Entities;

public class CajaAhorro
{
    public int Id { get; set; }
    public int EmpresaId { get; set; }
    public string Nombre { get; set; } = string.Empty;

    /// <summary>Cuenta origen para la dispersión SPEI.</summary>
    public string CuentaRetiro { get; set; } = string.Empty;

    /// <summary>Tasa mensual por defecto para préstamos (0.01 a 0.02).</summary>
    public decimal TasaInteresMensual { get; set; }

    /// <summary>Porcentaje del salario quincenal que se descuenta como ahorro ordinario.</summary>
    public decimal PorcentajeAhorro { get; set; }

    public bool Activa { get; set; } = true;

    public Empresa? Empresa { get; set; }
    public ICollection<Ahorrador> Ahorradores { get; set; } = new List<Ahorrador>();
}
