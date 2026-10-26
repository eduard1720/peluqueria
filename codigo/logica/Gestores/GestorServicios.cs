using Logica.Datos;
using Logica.Modelos;
using Npgsql;

namespace Logica.Gestores;

public class GestorServicios
{
    private readonly ConexionBD conexion;

    public GestorServicios(ConexionBD conexion)
    {
        this.conexion = conexion;
    }

    public static List<Servicio> Referenciales() => new()
    {
        new Servicio(1, "Corte de cabello", "Corte clásico o moderno", 30, 35m, "corte.jpg"),
        new Servicio(2, "Perfilado de barba", "Perfilado y arreglo de barba", 30, 25m, "barba.jpg"),
        new Servicio(3, "Corte + barba", "Corte de cabello y perfilado de barba", 60, 55m, "corte-barba.jpg"),
        new Servicio(4, "Diseño de cejas", "Perfilado de cejas", 30, 15m, "cejas.jpg"),
        new Servicio(5, "Tinte", "Coloración completa", 90, 120m, "tinte.jpg"),
    };

    private const string ErrorDuplicado = "23505";   
    internal const string Columnas ="s.id, s.nombre, s.descripcion, s.duracion_min, s.precio, s.imagen, s.activo";

    public List<Servicio> ListarActivos() => Consultar("WHERE s.activo");

    public List<Servicio> ListarTodos() => Consultar("");

    public Servicio? ObtenerPorId(int id)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM servicios s WHERE s.id = @id", con);
        cmd.Parameters.AddWithValue("id", id);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        return r.Read() ? LeerServicio(r, 0) : null;
    }

    public Servicio Registrar(Servicio servicio)
    {
        try
        {
            using NpgsqlConnection con = conexion.Abrir();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO servicios (nombre, descripcion, duracion_min, precio, imagen, activo) " +
                "VALUES (@n, @d, @du, @p, @i, @a) RETURNING id", con);
            AgregarParametros(cmd, servicio);
            cmd.Parameters.AddWithValue("a", servicio.Activo);
            servicio.AsignarId((int)cmd.ExecuteScalar()!);
            return servicio;
        }
        catch (PostgresException ex) when (ex.SqlState == ErrorDuplicado)
        {
            throw new ArgumentException("Ya existe un servicio con ese nombre.", ex);
        }
    }

    public void Modificar(Servicio servicio)
    {
        try
        {
            using NpgsqlConnection con = conexion.Abrir();
            using var cmd = new NpgsqlCommand(
                "UPDATE servicios SET nombre = @n, descripcion = @d, duracion_min = @du, precio = @p, imagen = @i WHERE id = @id", con);
            AgregarParametros(cmd, servicio);
            cmd.Parameters.AddWithValue("id", servicio.Id);
            if (cmd.ExecuteNonQuery() == 0) throw new ArgumentException("El servicio no existe.");
        }
        catch (PostgresException ex) when (ex.SqlState == ErrorDuplicado)
        {
            throw new ArgumentException("Ya existe otro servicio con ese nombre.", ex);
        }
    }

    public void CambiarEstado(int idServicio, bool activo)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand("UPDATE servicios SET activo = @a WHERE id = @id", con);
        cmd.Parameters.AddWithValue("a", activo);
        cmd.Parameters.AddWithValue("id", idServicio);
        if (cmd.ExecuteNonQuery() == 0) throw new ArgumentException("El servicio no existe.");
    }

    private static void AgregarParametros(NpgsqlCommand cmd, Servicio servicio)
    {
        cmd.Parameters.AddWithValue("n", servicio.Nombre);
        cmd.Parameters.AddWithValue("d", servicio.Descripcion);
        cmd.Parameters.AddWithValue("du", servicio.DuracionMin);
        cmd.Parameters.AddWithValue("p", servicio.Precio);
        cmd.Parameters.AddWithValue("i", servicio.Imagen);
    }

    internal static Servicio LeerServicio(NpgsqlDataReader r, int i) =>
        new(r.GetInt32(i), r.GetString(i + 1), r.GetString(i + 2), r.GetInt32(i + 3), r.GetDecimal(i + 4),
            r.GetString(i + 5), r.GetBoolean(i + 6));

    private List<Servicio> Consultar(string filtro)
    {
        var lista = new List<Servicio>();
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM servicios s {filtro} ORDER BY s.id", con);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(LeerServicio(r, 0));
        return lista;
    }
}
