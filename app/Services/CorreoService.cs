using System.Net;
using System.Net.Mail;
namespace Controlex.Web.Services;

/// <summary>Envío de correo por SMTP. Configuración en la sección "Correo" de config/appsettings.Development.json.</summary>
public class CorreoService(IConfiguration configuracion)
{
    public async Task EnviarAsync(string destinatario, string asunto, string cuerpo)
    {
        var configuracionCorreo = configuracion.GetSection("Correo");
        var servidor = configuracionCorreo["Host"];
        if (string.IsNullOrWhiteSpace(servidor))
            throw new InvalidOperationException("El envío de correo no está configurado (sección Correo).");

        using var clienteSmtp = new SmtpClient(servidor, configuracionCorreo.GetValue("Puerto", 587))
        {
            EnableSsl = configuracionCorreo.GetValue("UsarSsl", true),
            Credentials = new NetworkCredential(configuracionCorreo["Usuario"], configuracionCorreo["Clave"])
        };
        var remitente = configuracionCorreo["Remitente"] ?? configuracionCorreo["Usuario"] ?? "";
        using var mensaje = new MailMessage(new MailAddress(remitente, "CONTROLEX"), new MailAddress(destinatario))
        {
            Subject = asunto,
            Body = cuerpo
        };
        await clienteSmtp.SendMailAsync(mensaje);
    }
}
