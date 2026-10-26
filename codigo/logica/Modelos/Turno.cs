namespace Logica.Modelos;

// Tramo continuo de atención (por ejemplo, 09:00-12:00).
public class Turno
{
    private TimeOnly inicio;
    private TimeOnly fin;

    public Turno(TimeOnly inicio, TimeOnly fin)
    {
        if (fin <= inicio) throw new ArgumentException("El fin del turno debe ser posterior al inicio.");
        this.inicio = inicio;
        this.fin = fin;
    }

    public TimeOnly Inicio => inicio;
    public TimeOnly Fin => fin;
}
