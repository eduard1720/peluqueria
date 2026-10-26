namespace Logica.Excepciones;

// Excepción propia: el horario elegido ya no está libre (RN-06, RNF-07).
public class HorarioNoDisponibleException : Exception
{
    public HorarioNoDisponibleException()
        : base("Ese horario ya no está disponible, elige otro.") { }

    public HorarioNoDisponibleException(string mensaje, Exception? causa = null)
        : base(mensaje, causa) { }
}
