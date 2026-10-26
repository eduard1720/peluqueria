using Logica.Datos;
using Logica.Modelos;
using Npgsql;

namespace Logica.Gestores;

public class GestorPeluqueros
{
    private readonly ConexionBD conexion;

    public GestorPeluqueros(ConexionBD conexion)
    {
        this.conexion = conexion;
    }

    internal const string Columnas = "p.id, p.nombre, p.apellido, p.especialidad, p.activo";

    public List<Peluquero> ListarActivos() => Consultar("WHERE p.activo");

    public List<Peluquero> ListarTodos() => Consultar("");

    public Peluquero? ObtenerPorId(int id)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM peluqueros p WHERE p.id = @id", con);
        cmd.Parameters.AddWithValue("id", id);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        return r.Read() ? LeerPeluquero(r, 0) : null;
    }

    public Peluquero Registrar(Peluquero peluquero)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO peluqueros (nombre, apellido, especialidad, activo) VALUES (@n, @a, @e, @ac) RETURNING id", con);
        AgregarParametros(cmd, peluquero);
        cmd.Parameters.AddWithValue("ac", peluquero.Activo);
        peluquero.AsignarId((int)cmd.ExecuteScalar()!);
        return peluquero;
    }

    public void Modificar(Peluquero peluquero)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand(
            "UPDATE peluqueros SET nombre = @n, apellido = @a, especialidad = @e WHERE id = @id", con);
        AgregarParametros(cmd, peluquero);
        cmd.Parameters.AddWithValue("id", peluquero.Id);
        if (cmd.ExecuteNonQuery() == 0) throw new ArgumentException("El peluquero no existe.");
    }

    public void CambiarEstado(int idPeluquero, bool activo)
    {
        using NpgsqlConnection con = conexion.Abrir();
        if (!activo)
        {
            using var contar = new NpgsqlCommand("SELECT COUNT(*) FROM peluqueros WHERE activo AND id <> @id", con);
            contar.Parameters.AddWithValue("id", idPeluquero);
            if ((long)contar.ExecuteScalar()! == 0) throw new ArgumentException("Debe quedar al menos un peluquero activo.");
        }
        using var cmd = new NpgsqlCommand("UPDATE peluqueros SET activo = @a WHERE id = @id", con);
        cmd.Parameters.AddWithValue("a", activo);
        cmd.Parameters.AddWithValue("id", idPeluquero);
        if (cmd.ExecuteNonQuery() == 0) throw new ArgumentException("El peluquero no existe.");
    }

    private static void AgregarParametros(NpgsqlCommand cmd, Peluquero peluquero)
    {
        cmd.Parameters.AddWithValue("n", peluquero.Nombre);
        cmd.Parameters.AddWithValue("a", peluquero.Apellido);
        cmd.Parameters.AddWithValue("e", peluquero.Especialidad);
    }

    internal static Peluquero LeerPeluquero(NpgsqlDataReader r, int i) =>
        new(r.GetInt32(i), r.GetString(i + 1), r.GetString(i + 2), r.GetString(i + 3), r.GetBoolean(i + 4));

    private List<Peluquero> Consultar(string filtro)
    {
        var lista = new List<Peluquero>();
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM peluqueros p {filtro} ORDER BY p.nombre, p.apellido", con);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(LeerPeluquero(r, 0));
        return lista;
    }
}
