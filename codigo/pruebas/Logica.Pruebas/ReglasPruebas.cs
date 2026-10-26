using Logica.Enumeraciones;
using Logica.Gestores;
using Logica.Modelos;
using Logica.Utilidades;

namespace Logica.Pruebas;

// Pruebas de reglas de negocio, validaciones, seguridad y reportes.
public class ReglasPruebas
{
    private static readonly DateOnly Lunes = new(2026, 10, 12);

    [Fact]
    public void Burbuja_OrdenaDeMayorAMenor()
    {
        var lista = new List<ConteoServicio> { new("Cejas", 2), new("Corte", 9), new("Tinte", 1), new("Barba", 5) };
        GeneradorReportes.OrdenarBurbuja(lista);
        Assert.Equal(new[] { "Corte", "Barba", "Cejas", "Tinte" }, lista.Select(c => c.Servicio));
    }

    [Fact]
    public void Reporte_CuentaEstadosIngresosYServicios()
    {
        var reservas = new List<Reserva>
        {
            Datos.Reserva(1, new DateOnly(2026, 10, 5), "09:00", Datos.Corte, EstadoReserva.Atendida),       // +35 real
            Datos.Reserva(2, new DateOnly(2026, 10, 5), "10:00", Datos.Corte, EstadoReserva.Cancelada),      // no cuenta en servicios
            Datos.Reserva(3, new DateOnly(2026, 10, 6), "09:00", Datos.Tinte, EstadoReserva.Confirmada),     // pasada sin marcar
            Datos.Reserva(4, new DateOnly(2026, 10, 13), "09:00", Datos.CorteBarba, EstadoReserva.Confirmada), // +55 estimado
        };
        Reporte r = GeneradorReportes.Generar(reservas, new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 31), Lunes);

        Assert.Equal(4, r.Total);
        Assert.Equal(35m, r.IngresoReal);
        Assert.Equal(55m, r.IngresoEstimado);
        Assert.Equal(1, r.SinMarcar);
        Assert.Equal(2, r.CitasPorDia[new DateOnly(2026, 10, 5)]);
        Assert.Equal(3, r.ServiciosOrdenados.Sum(c => c.Cantidad));
        Assert.Equal("reporte_20261001_20261031.txt", r.NombreArchivo);
    }

    [Fact]
    public void Reserva_NoSeCancelaSiYaEmpezoNiSeMarcaAtendidaAntesDeEmpezar()
    {
        var res = Datos.Reserva(1, Lunes, "10:00", Datos.Corte);
        Assert.Throws<InvalidOperationException>(() => res.Cancelar(new DateTime(2026, 10, 12, 10, 5, 0)));
        Assert.Throws<InvalidOperationException>(() => res.MarcarAtendida(new DateTime(2026, 10, 12, 9, 55, 0)));
        res.MarcarAtendida(new DateTime(2026, 10, 12, 10, 5, 0));
        Assert.Equal(EstadoReserva.Atendida, res.Estado);
        Assert.Throws<InvalidOperationException>(() => res.MarcarNoAsistio(new DateTime(2026, 10, 12, 11, 0, 0)));
    }

    [Fact]
    public void ReservaNueva_QuedaConfirmadaConPrecioCopiadoYHoraFin()
    {
        var res = new Reserva(Datos.Ana, Datos.Tinte, Datos.Luis, Lunes, new TimeOnly(14, 0), OrigenReserva.Web, null, true,
                              new DateTime(2026, 10, 11), null);
        Assert.Equal(EstadoReserva.Confirmada, res.Estado);
        Assert.Equal(new TimeOnly(15, 30), res.HoraFin);
        Assert.Equal(120m, res.Precio);
        Assert.False(res.CorreoEnviado);
    }

    [Theory]
    [InlineData("71234567", true)]
    [InlineData("+591 71234567", true)]
    [InlineData("61234567", true)]
    [InlineData("51234567", false)]
    [InlineData("7123456", false)]
    public void Telefono_SeValidaYNormaliza(string telefono, bool valido) =>
        Assert.Equal(valido, Validador.EsTelefonoValido(telefono));

    [Fact]
    public void Correo_ObligatorioEnLaWebYOpcionalEnLaReservaManual()
    {
        var datos = new DatosReserva { PeluqueroId = 1, Nombre = "Ana", Apellido = "Pérez", Telefono = "71234567", Correo = "" };
        Assert.Contains("El correo es obligatorio.", Validador.ValidarDatosReserva(datos, OrigenReserva.Web));
        Assert.Empty(Validador.ValidarDatosReserva(datos, OrigenReserva.Manual));
    }

    [Fact]
    public void Reserva_SinPeluqueroNoSeAcepta()
    {
        var datos = new DatosReserva { Nombre = "Ana", Apellido = "Pérez", Telefono = "71234567", Correo = "ana@correo.com" };
        Assert.Contains("Elige un peluquero.", Validador.ValidarDatosReserva(datos, OrigenReserva.Web));
    }

    [Fact]
    public void Peluquero_ValidaNombreYEspecialidad()
    {
        Assert.Throws<ArgumentException>(() => new Peluquero(0, "L", "Rojas", ""));
        Assert.Throws<ArgumentException>(() => new Peluquero(0, "Luis", "Rojas", new string('x', 61)));
        var p = new Peluquero(0, " Luis ", "Rojas", "  Barba ");
        Assert.Equal("Luis Rojas", p.NombreCompleto);
        Assert.Equal("Barba", p.Especialidad);
        p.Desactivar();
        Assert.False(p.Activo);
    }

    [Fact]
    public void Contrasena_SeVerificaConSuSalYNuncaSeGuardaEnTextoPlano()
    {
        string guardado = Seguridad.CalcularHash("Clave2026");
        Assert.DoesNotContain("Clave2026", guardado);
        Assert.True(Seguridad.VerificarHash("Clave2026", guardado));
        Assert.False(Seguridad.VerificarHash("otra2026", guardado));
        Assert.NotEqual(guardado, Seguridad.CalcularHash("Clave2026"));   // sal distinta cada vez
    }

    [Fact]
    public void InicioDeSesion_TresIntentosFallidosBloqueanCincoMinutos()
    {
        var u = new Usuario(1, "Luis", "Rojas", "luis.rojas", Seguridad.CalcularHash("Clave2026"), Rol.Recepcionista);
        DateTime ahora = new(2026, 10, 12, 9, 0, 0);

        Assert.False(GestorUsuarios.VerificarIntento(u, "mala1", ahora));
        Assert.False(GestorUsuarios.VerificarIntento(u, "mala2", ahora));
        Assert.False(GestorUsuarios.VerificarIntento(u, "mala3", ahora));
        Assert.True(u.EstaBloqueado(ahora));
        Assert.False(GestorUsuarios.VerificarIntento(u, "Clave2026", ahora.AddMinutes(4)));   // sigue bloqueada
        Assert.True(GestorUsuarios.VerificarIntento(u, "Clave2026", ahora.AddMinutes(6)));    // ya pasó el bloqueo
    }

    [Fact]
    public void Usuarios_SiempreQuedaUnAdministradorActivo()
    {
        var admin = new Usuario(1, "Eva", "Mendoza", "eva.mendoza", "x:y", Rol.Administrador);
        var otro = new Usuario(2, "Luis", "Rojas", "luis.rojas", "x:y", Rol.Recepcionista);

        Assert.NotNull(GestorUsuarios.ValidarCambio(admin, Rol.Administrador, false, 2, 1));   // último Administrador
        Assert.NotNull(GestorUsuarios.ValidarCambio(admin, Rol.Recepcionista, true, 2, 1));    // quitarle el rol también cuenta
        Assert.NotNull(GestorUsuarios.ValidarCambio(otro, Rol.Recepcionista, false, 2, 1));    // no puede desactivarse a sí mismo
        Assert.Null(GestorUsuarios.ValidarCambio(admin, Rol.Recepcionista, true, 2, 2));       // hay otro Administrador
        Assert.Null(GestorUsuarios.ValidarCambio(otro, Rol.Recepcionista, false, 1, 1));
        Assert.False(GestorUsuarios.PuedeDesactivar(admin, otro, 1));
    }

    [Theory]
    [InlineData(45, 30)]    // no es múltiplo de 30
    [InlineData(210, 30)]   // más de 180 minutos
    [InlineData(30, 0)]     // precio no positivo
    public void Servicio_ValidaDuracionYPrecio(int duracion, int precio) =>
        Assert.Throws<ArgumentException>(() => new Servicio(0, "Prueba", "", duracion, precio, ""));

    [Fact]
    public void Cliente_NormalizaElTelefonoYRechazaDatosNoValidos()
    {
        var c = new Cliente(0, "Ana", "Pérez", "+591 7123-4567", null, new DateTime(2026, 10, 1));
        Assert.Equal("71234567", c.Telefono);
        Assert.Throws<ArgumentException>(() => c.CambiarCorreo("sin-arroba"));
        Assert.Throws<ArgumentException>(() => c.CambiarNombre("A1", "Pérez"));
        c.CompletarCorreo("ana@correo.com");
        c.CompletarCorreo("otro@correo.com");          // la web no sobrescribe un correo existente
        Assert.Equal("ana@correo.com", c.Correo);
    }

    [Fact]
    public void Reporte_SeparaCitasEIngresosPorPeluquero()
    {
        var reservas = new List<Reserva>
        {
            Datos.Reserva(1, Lunes, "09:00", Datos.Corte, EstadoReserva.Atendida, Datos.Luis),        // Luis: +35
            Datos.Reserva(2, Lunes, "09:00", Datos.CorteBarba, EstadoReserva.Atendida, Datos.Marco),  // Marco a la misma hora: +55
            Datos.Reserva(3, Lunes, "10:00", Datos.Corte, EstadoReserva.Cancelada, Datos.Marco),      // cancelada: no cuenta
        };
        Reporte r = GeneradorReportes.Generar(reservas, Lunes, Lunes, Lunes);

        Assert.Equal(1, r.CitasPorPeluquero["Luis Rojas"]);
        Assert.Equal(1, r.CitasPorPeluquero["Marco Vaca"]);
        Assert.Equal(35m, r.IngresoPorPeluquero["Luis Rojas"]);
        Assert.Equal(55m, r.IngresoPorPeluquero["Marco Vaca"]);
        Assert.Contains("Marco Vaca", r.ATexto());
    }

    [Fact]
    public void Reporte_RangoInvertidoNoSePermite() =>
        Assert.Throws<ArgumentException>(() =>
            GeneradorReportes.Generar(new List<Reserva>(), new DateOnly(2026, 10, 31), new DateOnly(2026, 10, 1), Lunes));

    [Fact]
    public void BusquedaLineal_EncuentraPorNombreOTelefono()
    {
        var clientes = new List<Cliente>
        {
            Datos.Ana,
            new(2, "Bruno", "Suárez", "69876543", null, new DateTime(2026, 10, 2)),
        };
        Assert.Single(GestorClientes.BuscarEnLista(clientes, "ana"));
        Assert.Single(GestorClientes.BuscarEnLista(clientes, "698"));
        Assert.Empty(GestorClientes.BuscarEnLista(clientes, "carla"));
    }
}
