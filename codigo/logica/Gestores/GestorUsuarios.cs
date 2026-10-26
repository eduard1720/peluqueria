using Logica.Datos;
using Logica.Enumeraciones;
using Logica.Modelos;
using Logica.Utilidades;
using Npgsql;

namespace Logica.Gestores;

public class GestorUsuarios
{
    public const string Mensaje = "Usuario o contraseña incorrectos. Tras 3 intentos fallidos la cuenta se bloquea 5 minutos.";

    private const string ErrorDuplicado = "23505";   
    private const string Columnas =
        "id, nombre, apellido, nombre_usuario, contrasena_hash, rol, activo, intentos_fallidos, bloqueado_hasta";

    private readonly ConexionBD conexion;

    public GestorUsuarios(ConexionBD conexion)
    {
        this.conexion = conexion;
    }

    public Usuario? IniciarSesion(string nombreUsuario, string contrasena)
    {
        Usuario? usuario = BuscarPorNombreUsuario(nombreUsuario);
        bool acceso = VerificarIntento(usuario, contrasena, Reloj.AhoraBolivia());
        if (usuario != null) GuardarIntentos(usuario);  
        return acceso ? usuario : null;
    }

    public static bool VerificarIntento(Usuario? usuario, string contrasena, DateTime ahora)
    {
        if (usuario == null || !usuario.Activo || usuario.EstaBloqueado(ahora)) return false;
        if (Seguridad.VerificarHash(contrasena, usuario.ContrasenaHash))
        {
            usuario.ReiniciarIntentos();
            return true;
        }
        usuario.RegistrarIntentoFallido(ahora);
        return false;
    }

    public static bool PuedeDesactivar(Usuario objetivo, Usuario sesion, int administradoresActivos) =>
        objetivo.Id != sesion.Id && !(objetivo.Rol == Rol.Administrador && administradoresActivos <= 1);

    public static string? ValidarCambio(Usuario objetivo, Rol nuevoRol, bool nuevoActivo, int idSesion, int administradoresActivos)
    {
        if (objetivo.Id == idSesion && !nuevoActivo) return "No puedes desactivar tu propia cuenta.";
        if (objetivo.Id == idSesion && nuevoRol != objetivo.Rol) return "No puedes cambiar tu propio rol.";
        bool dejaDeSerAdministradorActivo = objetivo.EsAdministrador && objetivo.Activo &&
                                            (nuevoRol != Rol.Administrador || !nuevoActivo);
        if (dejaDeSerAdministradorActivo && administradoresActivos <= 1) return "Debe quedar al menos un Administrador activo.";
        return null;
    }


    public Usuario? BuscarPorNombreUsuario(string nombreUsuario)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM usuarios WHERE nombre_usuario = @u", con);
        cmd.Parameters.AddWithValue("u", (nombreUsuario ?? "").Trim());
        using NpgsqlDataReader r = cmd.ExecuteReader();
        return r.Read() ? LeerUsuario(r) : null;
    }

    public Usuario? ObtenerPorId(int id)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM usuarios WHERE id = @id", con);
        cmd.Parameters.AddWithValue("id", id);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        return r.Read() ? LeerUsuario(r) : null;
    }

    public List<Usuario> Listar()
    {
        var lista = new List<Usuario>();
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand($"SELECT {Columnas} FROM usuarios ORDER BY rol, apellido, nombre", con);
        using NpgsqlDataReader r = cmd.ExecuteReader();
        while (r.Read()) lista.Add(LeerUsuario(r));
        return lista;
    }

    public int ContarAdministradoresActivos()
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios WHERE rol = 'Administrador' AND activo", con);
        return (int)(long)cmd.ExecuteScalar()!;
    }

    public Usuario Crear(string nombre, string apellido, string nombreUsuario, string contrasena, Rol rol)
    {
        if (!Validador.EsContrasenaSegura(contrasena))
            throw new ArgumentException("La contraseña debe tener al menos 8 caracteres, con letras y números.");
        var usuario = new Usuario(0, nombre, apellido, (nombreUsuario ?? "").Trim(), Seguridad.CalcularHash(contrasena), rol);
        try
        {
            using NpgsqlConnection con = conexion.Abrir();
            using var cmd = new NpgsqlCommand(
                "INSERT INTO usuarios (nombre, apellido, nombre_usuario, contrasena_hash, rol, activo) " +
                "VALUES (@n, @a, @u, @h, @r, TRUE) RETURNING id", con);
            cmd.Parameters.AddWithValue("n", usuario.Nombre);
            cmd.Parameters.AddWithValue("a", usuario.Apellido);
            cmd.Parameters.AddWithValue("u", usuario.NombreUsuario);
            cmd.Parameters.AddWithValue("h", usuario.ContrasenaHash);
            cmd.Parameters.AddWithValue("r", usuario.Rol.ToString());
            usuario.AsignarId((int)cmd.ExecuteScalar()!);
            return usuario;
        }
        catch (PostgresException ex) when (ex.SqlState == ErrorDuplicado)
        {
            throw new ArgumentException("Ese nombre de usuario ya existe.", ex);
        }
    }

    public void Modificar(Usuario usuario)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand(
            "UPDATE usuarios SET nombre = @n, apellido = @a, contrasena_hash = @h, rol = @r, activo = @ac, " +
            "intentos_fallidos = @i, bloqueado_hasta = @b WHERE id = @id", con);
        cmd.Parameters.AddWithValue("n", usuario.Nombre);
        cmd.Parameters.AddWithValue("a", usuario.Apellido);
        cmd.Parameters.AddWithValue("h", usuario.ContrasenaHash);
        cmd.Parameters.AddWithValue("r", usuario.Rol.ToString());
        cmd.Parameters.AddWithValue("ac", usuario.Activo);
        cmd.Parameters.AddWithValue("i", usuario.IntentosFallidos);
        cmd.Parameters.AddWithValue("b", usuario.BloqueadoHasta.HasValue ? ConexionBD.SinZona(usuario.BloqueadoHasta.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("id", usuario.Id);
        if (cmd.ExecuteNonQuery() == 0) throw new ArgumentException("El usuario no existe.");
    }

    
    public bool CrearAdministradorInicialSiNoHay(string nombreUsuario, string contrasena)
    {
        using (NpgsqlConnection con = conexion.Abrir())
        using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios", con))
        {
            if ((long)cmd.ExecuteScalar()! > 0) return false;
        }
        Crear("Administrador", "Inicial", nombreUsuario, contrasena, Rol.Administrador);
        return true;
    }

    
    private void GuardarIntentos(Usuario usuario)
    {
        using NpgsqlConnection con = conexion.Abrir();
        using var cmd = new NpgsqlCommand(
            "UPDATE usuarios SET intentos_fallidos = @i, bloqueado_hasta = @b WHERE id = @id", con);
        cmd.Parameters.AddWithValue("i", usuario.IntentosFallidos);
        cmd.Parameters.AddWithValue("b", usuario.BloqueadoHasta.HasValue ? ConexionBD.SinZona(usuario.BloqueadoHasta.Value) : DBNull.Value);
        cmd.Parameters.AddWithValue("id", usuario.Id);
        cmd.ExecuteNonQuery();
    }

    private static Usuario LeerUsuario(NpgsqlDataReader r) =>
        new(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetString(3), r.GetString(4),
            Enum.Parse<Rol>(r.GetString(5)), r.GetBoolean(6), r.GetInt32(7), r.IsDBNull(8) ? null : r.GetDateTime(8));
}
