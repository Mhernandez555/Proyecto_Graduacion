using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
namespace Controlex.Web.Services;

/// <summary>Envío de correo mediante SMTP con STARTTLS.</summary>
public class CorreoService(IConfiguration configuracion)
{
    public async Task EnviarAsync(string destinatario, string asunto, string cuerpo)
    {
        var configuracionCorreo = configuracion.GetSection("Correo");
        var servidor = configuracionCorreo["Host"];
        var usuario = configuracionCorreo["Usuario"];
        var clave = configuracionCorreo["Clave"];
        var remitente = configuracionCorreo["Remitente"];
        if (string.IsNullOrWhiteSpace(servidor) ||
            string.IsNullOrWhiteSpace(usuario) ||
            string.IsNullOrWhiteSpace(clave) ||
            string.IsNullOrWhiteSpace(remitente))
            throw new InvalidOperationException("El envío de correo no está configurado (sección Correo).");

        using var clienteSmtp = new SmtpClient();
        await clienteSmtp.ConnectAsync(
            servidor,
            configuracionCorreo.GetValue("Puerto", 587),
            SecureSocketOptions.StartTls);
        await clienteSmtp.AuthenticateAsync(usuario, clave);

        var mensaje = new MimeMessage
        {
            Subject = asunto,
            Body = new TextPart("plain") { Text = cuerpo }
        };
        mensaje.From.Add(MailboxAddress.Parse(remitente));
        mensaje.To.Add(MailboxAddress.Parse(destinatario));
        await clienteSmtp.SendAsync(mensaje, CancellationToken.None, null);
        await clienteSmtp.DisconnectAsync(true);
    }
}
