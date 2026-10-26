using Logica.Datos;
using Logica.Modelos;
using Logica.Utilidades;
using Npgsql;

namespace Logica.Gestores;

public class GestorClientes
{
    private const string ErrorDuplicado = "23505";   
    private const string Columnas = "id, nombre, apellido, telefono, correo, fecha_registro";

    private readonly ConexionBD conexion;
    private readonly GestorReservas gestorReservas;

    public GestorClientes(ConexionBD conexion, GestorReservas gestorReservas)
    {
        this.conexion = conexion;
        this.gestorReservas = gestorReservas;
    }

    public static List<Cliente> BuscarEnLista(List<Cliente> clientes, string texto)
    {
        var encontrados = new List<Cliente>();
        string buscado = (texto ?? "").Trim().ToLower();
        string telefonoBuscado = Validador.NormalizarTelefono(texto);
        foreach (Cliente c in clientes)
        {
            bool coincideNombre = buscado.Length > 0 && c.NombreCompleto.ToLower().Contains(buscado);
            bool coincideTelefono = telefonoBuscado.Length > 0 && c.Telefono.Contains(telefonoBuscado);
            if (coincideNombre || coincideTelefono) encontrados.Add(c);
        }
        return encontrados;
    }


    public List<Cliente> Listar()
    {
        var lista = new List<Cliente>();
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM clientes ORDER BY apellido, nombre", con);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(LeerCliente(r));
        return lista;
    }

    public List<Cliente> Buscar(string texto) => BuscarEnLista(Listar(), texto);

    public Cliente? ObtenerPorId(int id)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM clientes WHERE id = @id", con);
        cmd.Parameters.AddWithValue("id", id);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        return r.Read() ? LeerCliente(r) : null;
    }

    public Cliente Registrar(Cliente cliente)
    {
        try
        {
            using NpgsqlConnection con = conexion.Abrir();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO clientes (nombre, apellido, telefono, correo, fecha_registro) VALUES (@n, @a, @t, @c, @f) RETURNING id", con);
            cmd.Parameters.AddWithValue("n", cliente.Nombre);
            cmd.Parameters.AddWithValue("a", cliente.Apellido);
            cmd.Parameters.AddWithValue("t", cliente.Telefono);
            cmd.Parameters.AddWithValue("c", (object?)cliente.Correo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("f", ConexionBD.SinZona(cliente.FechaRegistro));
            cliente.AsignarId((int)cmd.ExecuteScalar()!);
            return cliente;
        }
        catch (PostgresException ex) when (ex.SqlState == ErrorDuplicado)
        {
            throw new ArgumentException("Ya existe un cliente con ese teléfono.", ex);
        }
    }

    public void Modificar(Cliente cliente)
    {
        try
        {
            using NpgsqlConnection con = conexion.Abrir();
            using var cmd = new NpgsqlCommand(
                "UPDATE clientes SET nombre = @n, apellido = @a, telefono = @t, correo = @c WHERE id = @id", con);
            cmd.Parameters.AddWithValue("n", cliente.Nombre);
            cmd.Parameters.AddWithValue("a", cliente.Apellido);
            cmd.Parameters.AddWithValue("t", cliente.Telefono);
            cmd.Parameters.AddWithValue("c", (object?)cliente.Correo ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id", cliente.Id);
            if (cmd.ExecuteNonQuery() == 0) throw new ArgumentException("El cliente no existe.");
        }
        catch (PostgresException ex) when (ex.SqlState == ErrorDuplicado)
        {
            throw new ArgumentException("Ya existe otro cliente con ese teléfono.", ex);
        }
    }

    public void Eliminar(int idCliente)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM reservas WHERE cliente_id = @id", con))
        {
            cmd.Parameters.AddWithValue("id", idCliente);
            long reservas = (long)cmd.ExecuteScalar()!;
            if (reservas > 0) throw new InvalidOperationException("El cliente tiene reservas registradas: no se puede eliminar.");
        }
        using (var cmd = new NpgsqlCommand("DELETE FROM clientes WHERE id = @id", con))
        {
            cmd.Parameters.AddWithValue("id", idCliente);
            if (cmd.ExecuteNonQuery() == 0) throw new ArgumentException("El cliente no existe.");
        }
    }

    public List<Reserva> Historial(int idCliente) => gestorReservas.ObtenerPorCliente(idCliente);

    private static Cliente LeerCliente(NpgsqlDataReader r) =>
        new(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.IsDBNull(4) ? null : r.GetString(4), r.GetDateTime(5));
}
