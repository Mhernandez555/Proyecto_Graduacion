using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

public class AccountController(ControlexDbContext contexto, PermisoService permisos, BitacoraService bitacora) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Home") : View(new LoginViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vistalogin)
    {
        if (!ModelState.IsValid) return View(vistalogin);

        var usuario = await contexto.Usuarios.Include(usuario => usuario.Rol)
            .FirstOrDefaultAsync(usuario => usuario.NombreUsuario == vistalogin.NombreUsuario);
        if (usuario is null || !usuario.Estado || !usuario.Rol.Estado || !PasswordHasher.Verify(vistalogin.Contrasena, usuario.ContrasenaHash))
        {
            if (usuario is not null) await bitacora.MovimientoAsync(usuario.IdUsuario, "SEGURIDAD", "LOGIN FALLIDO", null);
            vistalogin.Contrasena = "";
            ModelState.Clear();
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");   // ventana de alerta (RF-001)
            return View(vistalogin);
        }

        var niveles = await permisos.NivelesAsync(usuario);
        var idIngreso = await bitacora.IngresoAsync(usuario.IdUsuario);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new(ClaimTypes.Name, usuario.NombreUsuario),
            new("nombre_completo", $"{usuario.Nombre} {usuario.Apellido1}"),
            new(ClaimTypes.Role, usuario.Rol.NombreRol),
            new("bitacora_ingreso", idIngreso.ToString()),
            new("requiere_cambio", usuario.RequiereCambioContrasena.ToString())
        };
        claims.AddRange(niveles.Select(nivel => new Claim("perm:" + nivel.Key, nivel.Value.ToString())));

        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
        await bitacora.MovimientoAsync(usuario.IdUsuario, "SEGURIDAD", "LOGIN", "Inicio de sesión");
        return RedirectToAction("Index", "Home");
    }

    [Authorize, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()   // RF-015
    {
        try
        {
            if (int.TryParse(User.FindFirstValue("bitacora_ingreso"), out var idIngreso))
                await bitacora.SalidaAsync(idIngreso, SesionConfig.MotivoCierreManual);
        }
        finally
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous] public IActionResult AccesoDenegado() => View();
}
