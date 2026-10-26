namespace Logica.Modelos;

// Peluquero (barbero) que atiende las citas. El cliente elige con quién atenderse (RN-21).
public class Peluquero : Persona
{
    private int id;
    private string especialidad = "";
    private bool activo;

    public Peluquero(int id, string nombre, string apellido, string especialidad, bool activo = true)
        : base(nombre, apellido)
    {
        this.id = id;
        CambiarEspecialidad(especialidad);
        this.activo = activo;
    }

    public int Id => id;
    public string Especialidad => especialidad;
    public bool Activo => activo;

    public void AsignarId(int nuevoId) => id = nuevoId;

    public void CambiarEspecialidad(string? nuevaEspecialidad)
    {
        string e = nuevaEspecialidad?.Trim() ?? "";
        if (e.Length > 60) throw new ArgumentException("La especialidad admite como máximo 60 caracteres.");
        especialidad = e;
    }

    // Borrado lógico (RN-16): un peluquero inactivo deja de aparecer en la web, pero conserva su historial.
    public void Activar() => activo = true;
    public void Desactivar() => activo = false;
}
