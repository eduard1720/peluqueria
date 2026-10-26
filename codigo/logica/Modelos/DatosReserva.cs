namespace Logica.Modelos;

// Datos que llegan del formulario de reserva (web o manual).
public class DatosReserva
{
    public int ServicioId { get; set; }
    public int PeluqueroId { get; set; }
    public DateOnly Fecha { get; set; }
    public TimeOnly Hora { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string? Correo { get; set; }
    public string? Notas { get; set; }
}
