using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

public class AuthController(ControlexDbContext contexto, PermisoService permisos, BitacoraService bitacora,
                            ILogger<AuthController> registrador) : Controller
{
    [AllowAnonymous, HttpGet]
    public IActionResult Login() =>
        User.Identity?.IsAuthenticated == true ? RedirectToAction("Index", "Dashboard") : View(new LoginViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel vistaLogin)
    {
        if (!ModelState.IsValid) return View(vistaLogin);

        var usuario = await contexto.Usuarios.Include(usuario => usuario.Rol)
            .FirstOrDefaultAsync(usuario => usuario.NombreUsuario == vistaLogin.NombreUsuario);
        if (usuario is null || !usuario.Estado || !usuario.Rol.Estado || !PasswordHasher.Verify(vistaLogin.Contrasena, usuario.ContrasenaHash))
        {
            if (usuario is not null) await bitacora.MovimientoAsync(usuario.IdUsuario, "SEGURIDAD", "LOGIN FALLIDO", null);
            vistaLogin.Contrasena = "";
            ModelState.Clear();
            ModelState.AddModelError("", "Usuario o contraseña incorrectos.");   // ventana de alerta (RF-001)
            return View(vistaLogin);
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
        return RedirectToAction("Index", "Dashboard");
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

    // RF-023: recuperar contraseña
    [AllowAnonymous, HttpGet]
    public IActionResult RecuperarContrasena() => View(new RecuperarContrasenaViewModel());

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RecuperarContrasena(RecuperarContrasenaViewModel vistaRecuperacion, [FromServices] CorreoService servicioCorreo)
    {
        if (!ModelState.IsValid) return View(vistaRecuperacion);

        var usuario = await contexto.Usuarios.FirstOrDefaultAsync(usuario =>
            usuario.NombreUsuario == vistaRecuperacion.NombreUsuario &&
            usuario.Cedula == vistaRecuperacion.Cedula && usuario.Estado);
        if (usuario is null)
        {
            ModelState.AddModelError("", "Los datos ingresados no coinciden con un usuario activo.");
            return View(vistaRecuperacion);
        }

        var temporal = PasswordPolicy.GenerarTemporal();
        try
        {
            await servicioCorreo.EnviarAsync(usuario.Correo, "CONTROLEX: contraseña temporal",
                $"Hola {usuario.Nombre},\n\nSu contraseña temporal es: {temporal}\n\n" +
                "Ingrese al sistema con ella y cámbiela cuando se le solicite.\n\nCONTROLEX – WallRed");
        }
        catch (Exception ex)
        {
            registrador.LogError(ex, "No se pudo enviar el correo de recuperación");
            ModelState.AddModelError("", "No se pudo enviar el correo. Intente más tarde o comuníquese con soporte.");
            return View(vistaRecuperacion);
        }

        usuario.ContrasenaHash = PasswordHasher.Hash(temporal);
        usuario.RequiereCambioContrasena = true;
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(usuario.IdUsuario, "SEGURIDAD", "RECUPERAR CONTRASEÑA", "Se envió una contraseña temporal al correo registrado");

        TempData["Mensaje"] = "Se envió una contraseña temporal al correo registrado.";
        return RedirectToAction(nameof(Login));
    }

    [AllowAnonymous] public IActionResult AccesoDenegado() => View();
}
