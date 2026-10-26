using Npgsql;

namespace Logica.Datos;

// Abre conexiones a Supabase (PostgreSQL) con la cadena del "Session pooler".
// La cadena NO se escribe en el código: llega desde la configuración (user-secrets o el hosting).
public class ConexionBD
{
    private readonly string cadena;

    public ConexionBD(string cadena)
    {
        this.cadena = cadena;
    }

    public NpgsqlConnection Abrir()
    {
        if (string.IsNullOrWhiteSpace(cadena))
            throw new InvalidOperationException("Falta configurar la cadena de conexión de Supabase (ConnectionStrings:Supabase).");
        var conexion = new NpgsqlConnection(cadena);
        conexion.Open();
        return conexion;
    }

    // Las columnas TIMESTAMP no tienen zona: la hora ya es la de Bolivia (RN-10).
    public static DateTime SinZona(DateTime fecha) => DateTime.SpecifyKind(fecha, DateTimeKind.Unspecified);
}
