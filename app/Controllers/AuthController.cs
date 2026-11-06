using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

public class AuthController(ControlexDbContext db, PermisoService permisos, BitacoraService bitacora,
                            ILogger<AuthController> log) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Dashboard") : View(new LoginViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel m)
    {
        if (!ModelState.IsValid) return View(m);

        var u = await db.Usuarios.Include(x => x.Rol).FirstOrDefaultAsync(x => x.NombreUsuario == m.NombreUsuario);
        if (u is null || !u.Estado || !u.Rol.Estado || !PasswordHasher.Verify(m.Contrasena, u.ContrasenaHash))
        {
            if (u is not null) await bitacora.MovimientoAsync(u.IdUsuario, "SEGURIDAD", "LOGIN FALLIDO", null);
            m.Contrasena = "";
            ModelState.Clear();
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");   // ventana de alerta (RF-001)
            return View(m);
        }

        var niveles = await permisos.NivelesAsync(u);
        var idIngreso = await bitacora.IngresoAsync(u.IdUsuario);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, u.IdUsuario.ToString()),
            new(ClaimTypes.Name, u.NombreUsuario),
            new("nombre_completo", $"{u.Nombre} {u.Apellido1}"),
            new(ClaimTypes.Role, u.Rol.NombreRol),
            new("bitacora_ingreso", idIngreso.ToString()),
            new("requiere_cambio", u.RequiereCambioContrasena.ToString())
        };
        claims.AddRange(niveles.Select(n => new Claim("perm:" + n.Key, n.Value.ToString())));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        await bitacora.MovimientoAsync(u.IdUsuario, "SEGURIDAD", "LOGIN", "Inicio de sesión");
        return RedirectToAction("Index", "Dashboard");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()   // RF-015
    {
        if (int.TryParse(User.FindFirstValue("bitacora_ingreso"), out var id)) await bitacora.SalidaAsync(id);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    // RF-023: recuperar contraseña
    [AllowAnonymous, HttpGet]
    public IActionResult RecuperarContrasena() => View(new RecuperarContrasenaViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecuperarContrasena(RecuperarContrasenaViewModel m, [FromServices] CorreoService correo)
    {
        if (!ModelState.IsValid) return View(m);

        var u = await db.Usuarios.FirstOrDefaultAsync(x => x.NombreUsuario == m.NombreUsuario && x.Cedula == m.Cedula && x.Estado);
        if (u is null)
        {
            ModelState.AddModelError("", "Los datos ingresados no coinciden con un usuario activo.");
            return View(m);
        }

        var temporal = PasswordPolicy.GenerarTemporal();
        try
        {
            await correo.EnviarAsync(u.Correo, "CONTROLEX: contraseña temporal",
                $"Hola {u.Nombre},\n\nSu contraseña temporal es: {temporal}\n\n" +
                "Ingrese al sistema con ella y cámbiela cuando se le solicite.\n\nCONTROLEX – WallRed");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "No se pudo enviar el correo de recuperación");
            ModelState.AddModelError("", "No se pudo enviar el correo. Intente más tarde o comuníquese con soporte.");
            return View(m);
        }

        u.ContrasenaHash = PasswordHasher.Hash(temporal);
        u.RequiereCambioContrasena = true;
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(u.IdUsuario, "SEGURIDAD", "RECUPERAR CONTRASEÑA", "Se envió una contraseña temporal al correo registrado");

        TempData["Mensaje"] = "Se envió una contraseña temporal al correo registrado.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous] public IActionResult AccesoDenegado() => View();
}
