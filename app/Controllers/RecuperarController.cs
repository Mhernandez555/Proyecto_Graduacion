using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Controlex.Web.Controllers;

[AllowAnonymous]
public class RecuperarController(
    RecuperacionService recuperacion,
    CorreoService correo,
    ILogger<RecuperarController> registrador) : Controller
{
    [HttpGet]
    public IActionResult Index() => View(new RecuperarContrasenaViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(RecuperarContrasenaViewModel modelo)
    {
        if (!ModelState.IsValid) return View(modelo);

        var datos = await recuperacion.ObtenerDatosAsync(modelo.NombreUsuario, modelo.Cedula);
        if (datos is null)
        {
            ModelState.AddModelError("", "Los datos no coinciden con un usuario activo");
            return View(modelo);
        }

        var temporal = RecuperacionService.GenerarTemporal();
        try
        {
            await correo.EnviarAsync(
                datos.Correo,
                "CONTROLEX: contraseña temporal",
                $"Hola {datos.Nombre},\n\nSu contraseña temporal es: {temporal}\n\n" +
                "Ingrese al sistema con ella y cámbiela cuando se le solicite.");
        }
        catch (Exception ex)
        {
            registrador.LogError(ex, "No se pudo enviar el correo de recuperación.");
            ModelState.AddModelError("", "No se pudo enviar el correo. Intente nuevamente");
            return View(modelo);
        }

        var direccion = HttpContext.Connection.RemoteIpAddress;
        if (direccion?.IsIPv4MappedToIPv6 == true) direccion = direccion.MapToIPv4();
        var ip = direccion?.ToString() ?? "desconocida";

        try
        {
            var hash = PasswordHasher.Hash(temporal);
            if (!await recuperacion.AplicarAsync(modelo.NombreUsuario, modelo.Cedula, hash, ip))
            {
                ModelState.AddModelError("", "No se pudo completar la recuperación. Intente nuevamente");
                return View(modelo);
            }
        }
        catch (Exception ex)
        {
            registrador.LogError(ex, "No se pudo guardar la contraseña temporal de recuperación.");
            ModelState.AddModelError("", "No se pudo completar la recuperación. Intente nuevamente");
            return View(modelo);
        }

        TempData["Mensaje"] = "Se envió una contraseña temporal al correo registrado";
        return RedirectToAction("Login", "Auth");
    }
}
