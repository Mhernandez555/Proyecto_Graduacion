using System.Net;
using System.Net.Mail;
namespace Controlex.Web.Services;

/// <summary>Envío de correo por SMTP. Configuración en la sección "Correo" de config/appsettings.Development.json.</summary>
public class CorreoService(IConfiguration config)
{
    public async Task EnviarAsync(string para, string asunto, string cuerpo)
    {
        var s = config.GetSection("Correo");
        var host = s["Host"];
        if (string.IsNullOrWhiteSpace(host))
            throw new InvalidOperationException("El envío de correo no está configurado (sección Correo).");

        using var smtp = new SmtpClient(host, s.GetValue("Puerto", 587))
        {
            EnableSsl = s.GetValue("UsarSsl", true),
            Credentials = new NetworkCredential(s["Usuario"], s["Clave"])
        };
        var remitente = s["Remitente"] ?? s["Usuario"] ?? "";
        using var mensaje = new MailMessage(new MailAddress(remitente, "CONTROLEX"), new MailAddress(para))
        {
            Subject = asunto,
            Body = cuerpo
        };
        await smtp.SendMailAsync(mensaje);
    }
}
