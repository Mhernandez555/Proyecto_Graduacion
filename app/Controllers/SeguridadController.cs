using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
namespace Controlex.Web.Controllers;

public class SeguridadController(ControlexDbContext contexto, BitacoraService bitacora) : Controller
{
    // RF-022: cambiar contraseña
    [HttpGet]
    public IActionResult CambiarContrasena() => View(new CambiarContrasenaViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaViewModel modeloContrasena)
    {
        if (!ModelState.IsValid) return View(modeloContrasena);

        var idUsuario = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var usuario = await contexto.Usuarios.FindAsync(idUsuario);
        if (usuario is null || !PasswordHasher.Verify(modeloContrasena.ContrasenaActual, usuario.ContrasenaHash))
        {
            ModelState.AddModelError(nameof(modeloContrasena.ContrasenaActual), "La contraseña actual es incorrecta.");
            return View(modeloContrasena);
        }

        var error = PasswordPolicy.Validar(modeloContrasena.NuevaContrasena);
        if (error is not null)
        {
            ModelState.AddModelError(nameof(modeloContrasena.NuevaContrasena), error);
            return View(modeloContrasena);
        }
        if (modeloContrasena.NuevaContrasena == modeloContrasena.ContrasenaActual)
        {
            ModelState.AddModelError(nameof(modeloContrasena.NuevaContrasena), "La nueva contraseña debe ser distinta de la actual.");
            return View(modeloContrasena);
        }

        usuario.ContrasenaHash = PasswordHasher.Hash(modeloContrasena.NuevaContrasena);
        usuario.RequiereCambioContrasena = false;
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(usuario.IdUsuario, "SEGURIDAD", "CAMBIO DE CONTRASEÑA", null);

        // Reemite la sesión sin la marca de "debe cambiar contraseña"
        var identidad = new ClaimsIdentity(User.Claims.Where(claim => claim.Type != "requiere_cambio"),
                                           CookieAuthenticationDefaults.AuthenticationScheme);
        identidad.AddClaim(new Claim("requiere_cambio", "False"));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

        TempData["Mensaje"] = "Contraseña actualizada correctamente.";
        return RedirectToAction("Index", "Dashboard");
    }
}
