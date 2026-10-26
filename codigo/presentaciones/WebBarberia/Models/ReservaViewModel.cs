using Logica.Modelos;

namespace WebBarberia.Models;

// Datos que viajan entre los pasos 3, 4 y 5 de la reserva web.
public class ReservaViewModel
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

    public Servicio? Servicio { get; set; }        // solo para mostrar; se vuelven a leer de la base de datos
    public Peluquero? Peluquero { get; set; }
    public List<string> Errores { get; set; } = new();

    public DatosReserva ADatosReserva() => new()
    {
        ServicioId = ServicioId, PeluqueroId = PeluqueroId, Fecha = Fecha, Hora = Hora, Nombre = Nombre ?? "", Apellido = Apellido ?? "",
        Telefono = Telefono ?? "", Correo = Correo, Notas = Notas,
    };
}
