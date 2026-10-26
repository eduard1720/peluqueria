namespace Logica.Utilidades;

// Registro de errores en un archivo de texto (RF-22 guardar, RF-23 leer).
// Cada línea: "dd/MM/yyyy HH:mm:ss | origen | mensaje".
public class RegistroErrores
{
    private static readonly object Candado = new();
    private readonly string rutaArchivo;

    public RegistroErrores(string carpeta)
    {
        Directory.CreateDirectory(carpeta);
        rutaArchivo = Path.Combine(carpeta, "errores.txt");
    }

    public string RutaArchivo => rutaArchivo;

    public void Registrar(string origen, string mensaje)
    {
        try
        {
            string linea = $"{Reloj.AhoraBolivia():dd/MM/yyyy HH:mm:ss} | {origen} | {mensaje.Replace(Environment.NewLine, " ")}";
            lock (Candado)
            {
                File.AppendAllText(rutaArchivo, linea + Environment.NewLine);
            }
        }
        catch (Exception)
        {
            // Si el propio registro falla, se ignora para no interrumpir al usuario (RNF-08).
        }
    }

    public void Registrar(string origen, Exception ex) => Registrar(origen, ex.Message);

    // Devuelve las últimas líneas, la más nueva primero.
    public List<string> LeerUltimas(int cantidad = 100)
    {
        if (!File.Exists(rutaArchivo)) return new List<string>();
        string[] lineas;
        lock (Candado)
        {
            lineas = File.ReadAllLines(rutaArchivo);
        }
        return lineas.Reverse().Take(cantidad).ToList();
    }
}
