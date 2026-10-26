using Logica.Modelos;

namespace WebBarberia.Models;

// Formulario de registro y modificación de peluqueros (RF-25).
public class PeluqueroViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string? Especialidad { get; set; }

    public List<string> Errores { get; set; } = new();

    public static PeluqueroViewModel Desde(Peluquero p) => new()
    {
        Id = p.Id, Nombre = p.Nombre, Apellido = p.Apellido, Especialidad = p.Especialidad,
    };
}
