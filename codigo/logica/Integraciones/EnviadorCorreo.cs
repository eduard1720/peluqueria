using System.Net.Http.Json;
using Logica.Modelos;
using Logica.Utilidades;

namespace Logica.Integraciones;

// Envía el correo de confirmación por la API de Brevo (RF-07). La clave llega desde la configuración.
public class EnviadorCorreo
{
    private const string UrlBrevo = "https://api.brevo.com/v3/smtp/email";
    private static readonly HttpClient Cliente = new() { Timeout = TimeSpan.FromSeconds(10) };   // RNF-09

    private readonly string claveApi;
    private readonly string correoRemitente;
    private readonly string nombreRemitente;
    private readonly string telefonoBarberia;
    private readonly RegistroErrores registro;

    public EnviadorCorreo(string claveApi, string correoRemitente, string nombreRemitente, string telefonoBarberia, RegistroErrores registro)
    {
        this.claveApi = claveApi;
        this.correoRemitente = correoRemitente;
        this.nombreRemitente = nombreRemitente;
        this.telefonoBarberia = telefonoBarberia;
        this.registro = registro;
    }

    // Devuelve true si Brevo aceptó el correo; si falla, registra el error y devuelve false (RN-20).
    public bool EnviarConfirmacion(Reserva reserva, string correoDestino)
    {
        if (string.IsNullOrWhiteSpace(claveApi) || string.IsNullOrWhiteSpace(correoRemitente))
        {
            registro.Registrar("EnviadorCorreo", "Correo no configurado (falta la clave de Brevo o el remitente).");
            return false;
        }
        try
        {
            var mensaje = new
            {
                sender = new { name = nombreRemitente, email = correoRemitente },
                to = new[] { new { email = correoDestino, name = reserva.Cliente.NombreCompleto } },
                subject = "Tu cita en As bajo la manga está confirmada",
                htmlContent = ArmarCuerpo(reserva),
            };
            using var pedido = new HttpRequestMessage(HttpMethod.Post, UrlBrevo) { Content = JsonContent.Create(mensaje) };
            pedido.Headers.Add("api-key", claveApi);
            using HttpResponseMessage respuesta = Cliente.Send(pedido);
            if (respuesta.IsSuccessStatusCode) return true;
            registro.Registrar("EnviadorCorreo", $"Brevo respondió {(int)respuesta.StatusCode}");
            return false;
        }
        catch (Exception ex)
        {
            registro.Registrar("EnviadorCorreo", ex);
            return false;
        }
    }

    private string ArmarCuerpo(Reserva r) =>
        $"<p>Hola {r.Cliente.Nombre}, tu cita quedó confirmada.</p>" +
        $"<p><b>Servicio:</b> {r.Servicio.Nombre}<br><b>Peluquero:</b> {r.Peluquero.NombreCompleto}<br><b>Fecha:</b> {r.Fecha:dd/MM/yyyy}<br>" +
        $"<b>Hora:</b> {r.HoraInicio:HH\\:mm}<br><b>Precio:</b> Bs {r.Precio:0.00}</p>" +
        $"<p>Para cancelar o cambiar tu cita, escríbenos al {telefonoBarberia}.</p>";
}
