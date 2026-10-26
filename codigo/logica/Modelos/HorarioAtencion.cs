namespace Logica.Modelos;

// Horario de atención (RN-02, referencial) y parámetros de la agenda (RN-03, RN-08, RN-09).
public static class HorarioAtencion
{
    public const int MinutosBloque = 30;
    public const int DiasMaximos = 30;
    public const int MargenWebMinutos = 30;

    public static readonly List<Turno> Turnos = new()
    {
        new Turno(new TimeOnly(9, 0), new TimeOnly(12, 0)),   // mañana
        new Turno(new TimeOnly(14, 0), new TimeOnly(20, 0)),  // tarde
    };

    public static bool EsDiaLaborable(DateOnly fecha) => fecha.DayOfWeek != DayOfWeek.Sunday;
}
