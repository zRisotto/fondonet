using FondoNet.Domain.Enums;

namespace FondoNet.Domain.Entities;

public class Ahorrador
{
    public int Id { get; set; }
    public int CajaAhorroId { get; set; }
    public string ClaveEmpleado { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Curp { get; set; } = string.Empty;
    public DateOnly FechaNacimiento { get; set; }
    public string Clabe { get; set; } = string.Empty;
    public EstatusAhorrador Estatus { get; set; } = EstatusAhorrador.Activo;

    /// <summary>Salario base mensual.</summary>
    public decimal SalarioBase { get; set; }

    /// <summary>Ahorro voluntario por quincena (sin aportación patronal).</summary>
    public decimal AhorroVoluntarioQuincenal { get; set; }

    public DateOnly FechaIngreso { get; set; }
    public DateOnly? FechaBaja { get; set; }

    public CajaAhorro? CajaAhorro { get; set; }
    public ICollection<MovimientoAhorro> Movimientos { get; set; } = new List<MovimientoAhorro>();
    public ICollection<Prestamo> Prestamos { get; set; } = new List<Prestamo>();
}
