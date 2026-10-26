using Logica.Enumeraciones;
using Logica.Gestores;
using Logica.Modelos;

namespace Logica.Pruebas;

// Pruebas de A1 (CalcularHorariosDisponibles) y A1.1 (HayChoque).
public class HorariosDisponiblesPruebas
{
    private static readonly DateOnly Lunes = new(2026, 10, 12);
    private static readonly DateTime DiaAnterior = new(2026, 10, 11, 10, 0, 0);

    [Fact]
    public void PruebaDeEscritorio_DevuelveLosNueveHorarios()
    {
        var R = new List<Reserva>
        {
            Datos.Reserva(10, Lunes, "10:00", Datos.CorteBarba),   // 10:00-11:00
            Datos.Reserva(11, Lunes, "15:00", Datos.Tinte),        // 15:00-16:30
        };

        var disponibles = GestorReservas.CalcularHorariosDisponibles(Lunes, Datos.CorteBarba, Datos.Luis.Id, R, 0, OrigenReserva.Web, DiaAnterior);

        Assert.Equal(new[] { "09:00", "11:00", "14:00", "16:30", "17:00", "17:30", "18:00", "18:30", "19:00" }, Datos.Horas(disponibles));
    }

    [Fact]
    public void HayChoque_CitasContiguasNoChocan()
    {
        var R = new List<Reserva> { Datos.Reserva(1, Lunes, "10:00", Datos.CorteBarba) };   // 10:00-11:00
        Assert.False(GestorReservas.HayChoque(new TimeOnly(9, 0), 60, R));    // 09:00-10:00
        Assert.False(GestorReservas.HayChoque(new TimeOnly(11, 0), 30, R));   // 11:00-11:30
        Assert.True(GestorReservas.HayChoque(new TimeOnly(10, 30), 30, R));   // dentro
        Assert.True(GestorReservas.HayChoque(new TimeOnly(9, 30), 60, R));    // se superpone al inicio
    }

    [Fact]
    public void ServicioQueCruzaElDescanso_NoSeOfrece()
    {
        var disponibles = GestorReservas.CalcularHorariosDisponibles(Lunes, Datos.Tinte, Datos.Luis.Id, new List<Reserva>(), 0, OrigenReserva.Web, DiaAnterior);
        Assert.DoesNotContain("11:00", Datos.Horas(disponibles));   // 11:00 + 90 min = 12:30 > 12:00
        Assert.Contains("10:30", Datos.Horas(disponibles));         // 10:30 + 90 min = 12:00
        Assert.DoesNotContain("19:00", Datos.Horas(disponibles));   // 19:00 + 90 min > 20:00
    }

    // RN-21: cada peluquero tiene su agenda; la cita de Luis no ocupa el horario de Marco.
    [Fact]
    public void ReservaDeOtroPeluquero_NoChoca()
    {
        var R = new List<Reserva> { Datos.Reserva(1, Lunes, "10:00", Datos.CorteBarba, peluquero: Datos.Luis) };   // Luis 10:00-11:00
        var deLuis = Datos.Horas(GestorReservas.CalcularHorariosDisponibles(Lunes, Datos.Corte, Datos.Luis.Id, R, 0, OrigenReserva.Web, DiaAnterior));
        var deMarco = Datos.Horas(GestorReservas.CalcularHorariosDisponibles(Lunes, Datos.Corte, Datos.Marco.Id, R, 0, OrigenReserva.Web, DiaAnterior));

        Assert.DoesNotContain("10:00", deLuis);
        Assert.DoesNotContain("10:30", deLuis);
        Assert.Contains("10:00", deMarco);
        Assert.Contains("10:30", deMarco);
    }

    [Fact]
    public void ReservaCanceladaNoOcupaHorario()
    {
        var R = new List<Reserva> { Datos.Reserva(1, Lunes, "09:00", Datos.Corte, EstadoReserva.Cancelada) };
        var disponibles = GestorReservas.CalcularHorariosDisponibles(Lunes, Datos.Corte, Datos.Luis.Id, R, 0, OrigenReserva.Web, DiaAnterior);
        Assert.Contains("09:00", Datos.Horas(disponibles));
    }

    [Fact]
    public void AlReprogramar_LaPropiaReservaNoSeCuenta()
    {
        var R = new List<Reserva> { Datos.Reserva(7, Lunes, "09:00", Datos.Corte) };
        var sinExcluir = GestorReservas.CalcularHorariosDisponibles(Lunes, Datos.Corte, Datos.Luis.Id, R, 0, OrigenReserva.Manual, DiaAnterior);
        var excluyendo = GestorReservas.CalcularHorariosDisponibles(Lunes, Datos.Corte, Datos.Luis.Id, R, 7, OrigenReserva.Manual, DiaAnterior);
        Assert.DoesNotContain("09:00", Datos.Horas(sinExcluir));
        Assert.Contains("09:00", Datos.Horas(excluyendo));
    }

    [Theory]
    [InlineData(2026, 10, 18)]   // domingo
    [InlineData(2026, 10, 10)]   // fecha pasada
    [InlineData(2026, 11, 11)]   // a 31 días
    public void FechasNoValidas_NoTienenHorarios(int a, int m, int d)
    {
        var disponibles = GestorReservas.CalcularHorariosDisponibles(new DateOnly(a, m, d), Datos.Corte, Datos.Luis.Id, new List<Reserva>(), 0,
                                                                     OrigenReserva.Web, DiaAnterior);
        Assert.Empty(disponibles);
    }

    [Fact]
    public void FechaATreintaDias_SiTieneHorarios()
    {
        var disponibles = GestorReservas.CalcularHorariosDisponibles(new DateOnly(2026, 11, 10), Datos.Corte, Datos.Luis.Id, new List<Reserva>(), 0,
                                                                     OrigenReserva.Web, DiaAnterior);
        Assert.NotEmpty(disponibles);
    }

    [Fact]
    public void Hoy_LaWebExigeTreintaMinutosYLaReservaManualPermiteElBloqueEnCurso()
    {
        DateTime ahora = new(2026, 10, 12, 10, 10, 0);
        var web = Datos.Horas(GestorReservas.CalcularHorariosDisponibles(Lunes, Datos.Corte, Datos.Luis.Id, new List<Reserva>(), 0, OrigenReserva.Web, ahora));
        var manual = Datos.Horas(GestorReservas.CalcularHorariosDisponibles(Lunes, Datos.Corte, Datos.Luis.Id, new List<Reserva>(), 0, OrigenReserva.Manual, ahora));

        Assert.Equal("11:00", web[0]);      // 10:10 + 30 min = 10:40 → el primer bloque es 11:00
        Assert.Equal("10:00", manual[0]);   // el bloque en curso (10:00-10:30) se ofrece al cliente que llega
    }
}
