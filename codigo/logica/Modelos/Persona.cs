using Logica.Utilidades;

namespace Logica.Modelos;

// Clase base abstracta: datos comunes de Cliente, Usuario y Peluquero.
public abstract class Persona
{
    private string nombre = "";
    private string apellido = "";

    protected Persona(string nombre, string apellido)
    {
        CambiarNombre(nombre, apellido);
    }

    public string Nombre => nombre;
    public string Apellido => apellido;
    public string NombreCompleto => $"{nombre} {apellido}";

    public void CambiarNombre(string nombre, string apellido)
    {
        if (!Validador.EsNombreValido(nombre)) throw new ArgumentException("Nombre no válido.");
        if (!Validador.EsNombreValido(apellido)) throw new ArgumentException("Apellido no válido.");
        this.nombre = nombre.Trim();
        this.apellido = apellido.Trim();
    }
}
