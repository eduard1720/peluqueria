using Logica.Enumeraciones;
using Logica.Modelos;

namespace WebBarberia.Models;

// Formulario de creación y modificación de usuarios del personal (RF-19).
public class UsuarioViewModel
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Apellido { get; set; } = "";
    public string NombreUsuario { get; set; } = "";
    public string? Contrasena { get; set; }      // al modificar, vacía = no se cambia
    public Rol Rol { get; set; } = Rol.Recepcionista;
    public bool Activo { get; set; } = true;

    public List<string> Errores { get; set; } = new();

    public static UsuarioViewModel Desde(Usuario u) => new()
    {
        Id = u.Id, Nombre = u.Nombre, Apellido = u.Apellido, NombreUsuario = u.NombreUsuario, Rol = u.Rol, Activo = u.Activo,
    };
}
