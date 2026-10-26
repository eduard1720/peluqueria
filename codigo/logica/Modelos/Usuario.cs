using Logica.Enumeraciones;
using Logica.Utilidades;

namespace Logica.Modelos;

// Usuario del personal (Administrador o Recepcionista).
public class Usuario : Persona
{
    public const int MaximoIntentos = 3;
    public const int MinutosBloqueo = 5;

    private int id;
    private string nombreUsuario = "";
    private string contrasenaHash = "";
    private Rol rol;
    private bool activo;
    private int intentosFallidos;
    private DateTime? bloqueadoHasta;

    public Usuario(int id, string nombre, string apellido, string nombreUsuario, string contrasenaHash, Rol rol,
                   bool activo = true, int intentosFallidos = 0, DateTime? bloqueadoHasta = null)
        : base(nombre, apellido)
    {
        if (!Validador.EsNombreUsuarioValido(nombreUsuario)) throw new ArgumentException("Nombre de usuario no válido.");
        this.id = id;
        this.nombreUsuario = nombreUsuario;
        this.contrasenaHash = contrasenaHash;
        this.rol = rol;
        this.activo = activo;
        this.intentosFallidos = intentosFallidos;
        this.bloqueadoHasta = bloqueadoHasta;
    }

    public int Id => id;
    public string NombreUsuario => nombreUsuario;
    public string ContrasenaHash => contrasenaHash;   // incluye la sal ("sal:hash")
    public Rol Rol => rol;
    public bool Activo => activo;
    public int IntentosFallidos => intentosFallidos;
    public DateTime? BloqueadoHasta => bloqueadoHasta;
    public bool EsAdministrador => rol == Rol.Administrador;

    public void AsignarId(int nuevoId) => id = nuevoId;

    public bool EstaBloqueado(DateTime ahora) => bloqueadoHasta.HasValue && bloqueadoHasta.Value > ahora;

    // Suma un intento fallido; al tercero bloquea la cuenta 5 minutos (RN-18). Devuelve true si quedó bloqueada.
    public bool RegistrarIntentoFallido(DateTime ahora)
    {
        intentosFallidos++;
        if (intentosFallidos >= MaximoIntentos)
        {
            bloqueadoHasta = ahora.AddMinutes(MinutosBloqueo);
            intentosFallidos = 0;
            return true;
        }
        return false;
    }

    public void ReiniciarIntentos()
    {
        intentosFallidos = 0;
        bloqueadoHasta = null;
    }

    public void CambiarContrasena(string nuevaContrasena)
    {
        if (!Validador.EsContrasenaSegura(nuevaContrasena)) throw new ArgumentException("La contraseña debe tener 8 caracteres con letras y números.");
        contrasenaHash = Seguridad.CalcularHash(nuevaContrasena);
    }

    public void CambiarRol(Rol nuevoRol) => rol = nuevoRol;
    public void Activar() => activo = true;
    public void Desactivar() => activo = false;
}
