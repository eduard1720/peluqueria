namespace Logica.Utilidades;

// Hora oficial del sistema (RN-10): Bolivia es UTC−4 todo el año (no tiene horario de verano).
// Nunca se usa DateTime.Now, porque el servidor puede estar en otro país.
public static class Reloj
{
    private const int DiferenciaHorasBolivia = -4;

    public static DateTime AhoraBolivia() => DateTime.UtcNow.AddHours(DiferenciaHorasBolivia);

    public static DateOnly HoyBolivia() => DateOnly.FromDateTime(AhoraBolivia());
}
