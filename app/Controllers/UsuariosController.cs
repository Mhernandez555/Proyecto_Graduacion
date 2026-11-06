using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

[RequiereModulo("USUARIOS", 1)]
public class UsuariosController(ControlexDbContext db, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index() =>
        View(await db.Usuarios.Include(u => u.Rol).OrderBy(u => u.Nombre).ThenBy(u => u.Apellido1).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarRoles();
        return View("Formulario", new UsuarioViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(UsuarioViewModel m)
    {
        if (string.IsNullOrEmpty(m.Contrasena)) ModelState.AddModelError(nameof(m.Contrasena), "Ingrese la contraseña.");
        else if (PasswordPolicy.Validar(m.Contrasena) is { } err) ModelState.AddModelError(nameof(m.Contrasena), err);
        await ValidarUnicos(m);
        if (!ModelState.IsValid) { await CargarRoles(); return View("Formulario", m); }

        var u = new Usuario
        {
            Cedula = m.Cedula, Nombre = m.Nombre.Trim(), Apellido1 = m.Apellido1.Trim(), Apellido2 = m.Apellido2?.Trim(),
            Correo = m.Correo.Trim(), IdRol = m.IdRol, NombreUsuario = m.NombreUsuario,
            ContrasenaHash = PasswordHasher.Hash(m.Contrasena!), RequiereCambioContrasena = true,
            CostoHora = m.CostoHora, Estado = m.Estado, FechaCreacion = DateTime.Now
        };
        db.Usuarios.Add(u);
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "USUARIOS", "INSERCIÓN", $"Usuario creado: {u.NombreUsuario}");

        TempData["Mensaje"] = "Usuario creado. Deberá cambiar su contraseña en el primer ingreso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();
        await CargarRoles();
        return View("Formulario", new UsuarioViewModel
        {
            IdUsuario = u.IdUsuario, Cedula = u.Cedula, Nombre = u.Nombre, Apellido1 = u.Apellido1, Apellido2 = u.Apellido2,
            Correo = u.Correo, IdRol = u.IdRol, NombreUsuario = u.NombreUsuario, CostoHora = u.CostoHora, Estado = u.Estado
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, UsuarioViewModel m)
    {
        m.IdUsuario = id;
        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();

        var cambiaClave = !string.IsNullOrEmpty(m.Contrasena);
        if (cambiaClave && PasswordPolicy.Validar(m.Contrasena!) is { } err) ModelState.AddModelError(nameof(m.Contrasena), err);
        if (id == IdActual && !m.Estado) ModelState.AddModelError(nameof(m.Estado), "No puede inhabilitar su propio usuario.");
        await ValidarUnicos(m);
        if (!ModelState.IsValid) { await CargarRoles(); return View("Formulario", m); }

        u.Cedula = m.Cedula; u.Nombre = m.Nombre.Trim(); u.Apellido1 = m.Apellido1.Trim(); u.Apellido2 = m.Apellido2?.Trim();
        u.Correo = m.Correo.Trim(); u.IdRol = m.IdRol; u.NombreUsuario = m.NombreUsuario;
        u.CostoHora = m.CostoHora; u.Estado = m.Estado;
        if (cambiaClave) { u.ContrasenaHash = PasswordHasher.Hash(m.Contrasena!); u.RequiereCambioContrasena = true; }
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "USUARIOS", "MODIFICACIÓN", $"Usuario modificado: {u.NombreUsuario}");

        TempData["Mensaje"] = "Usuario actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var u = await db.Usuarios.FindAsync(id);
        if (u is null) return NotFound();
        if (id == IdActual) { TempData["Error"] = "No puede inhabilitar su propio usuario."; return RedirectToAction(nameof(Index)); }

        u.Estado = !u.Estado;
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "USUARIOS", u.Estado ? "HABILITACIÓN" : "INHABILITACIÓN", $"Usuario: {u.NombreUsuario}");
        TempData["Mensaje"] = u.Estado ? "Usuario habilitado." : "Usuario inhabilitado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarUnicos(UsuarioViewModel m)
    {
        if (await db.Usuarios.AnyAsync(x => x.Cedula == m.Cedula && x.IdUsuario != m.IdUsuario))
            ModelState.AddModelError(nameof(m.Cedula), "Ya existe un usuario con esa cédula.");
        if (await db.Usuarios.AnyAsync(x => x.Correo == m.Correo && x.IdUsuario != m.IdUsuario))
            ModelState.AddModelError(nameof(m.Correo), "Ya existe un usuario con ese correo.");
        if (await db.Usuarios.AnyAsync(x => x.NombreUsuario == m.NombreUsuario && x.IdUsuario != m.IdUsuario))
            ModelState.AddModelError(nameof(m.NombreUsuario), "Ese nombre de usuario ya está en uso.");
        if (m.IdRol > 0 && !await db.Roles.AnyAsync(r => r.IdRol == m.IdRol && r.Estado))
            ModelState.AddModelError(nameof(m.IdRol), "Seleccione un rol activo.");
    }

    private async Task CargarRoles() =>
        ViewBag.Roles = new SelectList(await db.Roles.Where(r => r.Estado).OrderBy(r => r.NombreRol).ToListAsync(), "IdRol", "NombreRol");
}
