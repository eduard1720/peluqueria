using Logica.Modelos;

namespace WebBarberia.Models;

// Formulario de registro y modificación de clientes (RF-16).
public class ClienteViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string Telefono { get; set; } = "";
    public string? Correo { get; set; }

    public List<string> Errores { get; set; } = new();

    public static ClienteViewModel Desde(Cliente c) => new()
    {
        Id = c.Id, Nombre = c.Nombre, Apellido = c.Apellido, Telefono = c.Telefono, Correo = c.Correo,
    };
}
