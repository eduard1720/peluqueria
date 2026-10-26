using System.Text.RegularExpressions;
using Logica.Enumeraciones;
using Logica.Modelos;

namespace Logica.Utilidades;

// Reglas de validación de los datos de entrada (sección 6 del análisis).
public static class Validador
{
    private static readonly Regex SoloLetras = new(@"^[A-Za-zÁÉÍÓÚÜÑáéíóúüñ ]+$");
    private static readonly Regex Celular = new(@"^[67]\d{7}$");
    private static readonly Regex Correo = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    private static readonly Regex NombreUsuario = new(@"^[A-Za-z0-9._]{4,20}$");

    public static bool EsNombreValido(string? texto)
    {
        string t = (texto ?? "").Trim();
        return t.Length >= 2 && t.Length <= 50 && SoloLetras.IsMatch(t);
    }

    // Quita espacios, guiones y el prefijo +591 antes de guardar (RN-11).
    public static string NormalizarTelefono(string? telefono)
    {
        string t = (telefono ?? "").Replace(" ", "").Replace("-", "");
        if (t.StartsWith("+591")) t = t.Substring(4);
        return t;
    }

    public static bool EsTelefonoValido(string? telefono) => Celular.IsMatch(NormalizarTelefono(telefono));

    public static bool EsCorreoValido(string? correo)
    {
        string c = (correo ?? "").Trim();
        return c.Length <= 100 && Correo.IsMatch(c);
    }

    public static bool EsNombreUsuarioValido(string? usuario) => NombreUsuario.IsMatch(usuario ?? "");

    // Al menos 8 caracteres, con letras y números.
    public static bool EsContrasenaSegura(string? contrasena)
    {
        string c = contrasena ?? "";
        return c.Length >= 8 && c.Any(char.IsLetter) && c.Any(char.IsDigit);
    }

    // Devuelve la lista de errores de los datos de una reserva (vacía si todo está bien).
    public static List<string> ValidarDatosReserva(DatosReserva datos, OrigenReserva origen)
    {
        var errores = new List<string>();
        if (datos.PeluqueroId <= 0) errores.Add("Elige un peluquero.");
        if (!EsNombreValido(datos.Nombre)) errores.Add("El nombre debe tener de 2 a 50 letras.");
        if (!EsNombreValido(datos.Apellido)) errores.Add("El apellido debe tener de 2 a 50 letras.");
        if (!EsTelefonoValido(datos.Telefono)) errores.Add("El teléfono debe tener 8 dígitos y empezar con 6 o 7.");

        bool hayCorreo = !string.IsNullOrWhiteSpace(datos.Correo);
        if (origen == OrigenReserva.Web && !hayCorreo) errores.Add("El correo es obligatorio.");
        if (hayCorreo && !EsCorreoValido(datos.Correo)) errores.Add("El correo no tiene un formato válido.");

        if ((datos.Notas ?? "").Length > 200) errores.Add("Las notas admiten como máximo 200 caracteres.");
        return errores;
    }
}
