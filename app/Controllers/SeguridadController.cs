using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
namespace Controlex.Web.Controllers;

public class SeguridadController(ControlexDbContext db, BitacoraService bitacora) : Controller
{
    // RF-022: cambiar contraseña
    [HttpGet]
    public IActionResult CambiarContrasena() => View(new CambiarContrasenaViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarContrasena(CambiarContrasenaViewModel m)
    {
        if (!ModelState.IsValid) return View(m);

        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var u = await db.Usuarios.FindAsync(id);
        if (u is null || !PasswordHasher.Verify(m.ContrasenaActual, u.ContrasenaHash))
        {
            ModelState.AddModelError(nameof(m.ContrasenaActual), "La contraseña actual es incorrecta.");
            return View(m);
        }

        var error = PasswordPolicy.Validar(m.NuevaContrasena);
        if (error is not null) { ModelState.AddModelError(nameof(m.NuevaContrasena), error); return View(m); }
        if (m.NuevaContrasena == m.ContrasenaActual)
        {
            ModelState.AddModelError(nameof(m.NuevaContrasena), "La nueva contraseña debe ser distinta de la actual.");
            return View(m);
        }

        u.ContrasenaHash = PasswordHasher.Hash(m.NuevaContrasena);
        u.RequiereCambioContrasena = false;
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(u.IdUsuario, "SEGURIDAD", "CAMBIO DE CONTRASEÑA", null);

        // Reemite la sesión sin la marca de "debe cambiar contraseña"
        var identidad = new ClaimsIdentity(User.Claims.Where(c => c.Type != "requiere_cambio"),
                                           CookieAuthenticationDefaults.AuthenticationScheme);
        identidad.AddClaim(new Claim("requiere_cambio", "False"));
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identidad));

        TempData["Mensaje"] = "Contraseña actualizada correctamente.";
        return RedirectToAction("Index", "Dashboard");
    }
}
