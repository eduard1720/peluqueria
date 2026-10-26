using Logica.Utilidades;

namespace Logica.Modelos;

// Cliente de la barbería. Se identifica por su teléfono (RN-11).
public class Cliente : Persona
{
    private int id;
    private string telefono = "";
    private string? correo;
    private DateTime fechaRegistro;

    public Cliente(int id, string nombre, string apellido, string telefono, string? correo, DateTime fechaRegistro)
        : base(nombre, apellido)
    {
        this.id = id;
        CambiarTelefono(telefono);
        CambiarCorreo(correo);
        this.fechaRegistro = fechaRegistro;
    }

    public int Id => id;
    public string Telefono => telefono;
    public string? Correo => correo;
    public DateTime FechaRegistro => fechaRegistro;
    public bool TieneCorreo => !string.IsNullOrWhiteSpace(correo);

    public void AsignarId(int nuevoId) => id = nuevoId;

    public void CambiarTelefono(string nuevoTelefono)
    {
        if (!Validador.EsTelefonoValido(nuevoTelefono)) throw new ArgumentException("Teléfono no válido.");
        telefono = Validador.NormalizarTelefono(nuevoTelefono);
    }

    public void CambiarCorreo(string? nuevoCorreo)
    {
        if (string.IsNullOrWhiteSpace(nuevoCorreo)) { correo = null; return; }
        if (!Validador.EsCorreoValido(nuevoCorreo)) throw new ArgumentException("Correo no válido.");
        correo = nuevoCorreo.Trim();
    }

    // Reserva web: solo se completa el correo si estaba vacío; no se sobrescriben datos (RN-11).
    public void CompletarCorreo(string? nuevoCorreo)
    {
        if (!TieneCorreo && !string.IsNullOrWhiteSpace(nuevoCorreo)) CambiarCorreo(nuevoCorreo);
    }
}
