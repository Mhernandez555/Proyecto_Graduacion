using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

[RequiereModulo("ROLES_PERMISOS", 1)]
public class RolesController(ControlexDbContext db, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index() => View(await db.Roles.OrderBy(r => r.NombreRol).ToListAsync());

    [HttpGet]
    public IActionResult Crear()
    {
        var m = new RolViewModel();
        Normalizar(m);
        return View("Formulario", m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(RolViewModel m)
    {
        Normalizar(m);
        if (await db.Roles.AnyAsync(r => r.NombreRol == m.NombreRol))
            ModelState.AddModelError(nameof(m.NombreRol), "Ya existe un rol con ese nombre.");
        if (!ModelState.IsValid) return View("Formulario", m);

        var rol = new Rol { NombreRol = m.NombreRol.Trim(), Descripcion = m.Descripcion?.Trim(), EsAdministrador = m.EsAdministrador, Estado = m.Estado };
        db.Roles.Add(rol);
        await db.SaveChangesAsync();
        foreach (var p in m.Permisos)
            db.Permisos.Add(new Permiso { IdRol = rol.IdRol, Modulo = p.Modulo, NivelAcceso = (byte)p.Nivel });
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "ROLES_PERMISOS", "INSERCIÓN", $"Rol creado: {rol.NombreRol}");

        TempData["Mensaje"] = "Rol creado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var rol = await db.Roles.FindAsync(id);
        if (rol is null) return NotFound();
        var niveles = await db.Permisos.Where(p => p.IdRol == id).ToDictionaryAsync(p => p.Modulo, p => (int)p.NivelAcceso);
        var m = new RolViewModel
        {
            IdRol = rol.IdRol, NombreRol = rol.NombreRol, Descripcion = rol.Descripcion,
            EsAdministrador = rol.EsAdministrador, Estado = rol.Estado,
            Permisos = PermisoService.Modulos.Select(mod => new PermisoItemViewModel { Modulo = mod, Nivel = niveles.GetValueOrDefault(mod, 0) }).ToList()
        };
        return View("Formulario", m);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, RolViewModel m)
    {
        m.IdRol = id;
        Normalizar(m);
        var rol = await db.Roles.FindAsync(id);
        if (rol is null) return NotFound();

        if (await db.Roles.AnyAsync(r => r.NombreRol == m.NombreRol && r.IdRol != id))
            ModelState.AddModelError(nameof(m.NombreRol), "Ya existe un rol con ese nombre.");
        // Siempre debe quedar al menos un rol administrador activo
        var pierdeAdmin = rol.EsAdministrador && (!m.EsAdministrador || !m.Estado);
        if (pierdeAdmin && !await db.Roles.AnyAsync(r => r.IdRol != id && r.EsAdministrador && r.Estado))
            ModelState.AddModelError("", "Debe existir al menos un rol administrador activo.");
        if (!ModelState.IsValid) return View("Formulario", m);

        rol.NombreRol = m.NombreRol.Trim(); rol.Descripcion = m.Descripcion?.Trim();
        rol.EsAdministrador = m.EsAdministrador; rol.Estado = m.Estado;

        var existentes = await db.Permisos.Where(p => p.IdRol == id).ToListAsync();
        foreach (var p in m.Permisos)
        {
            var fila = existentes.FirstOrDefault(e => e.Modulo == p.Modulo);
            if (fila is null) db.Permisos.Add(new Permiso { IdRol = id, Modulo = p.Modulo, NivelAcceso = (byte)p.Nivel });
            else fila.NivelAcceso = (byte)p.Nivel;
        }
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "ROLES_PERMISOS", "MODIFICACIÓN", $"Rol modificado: {rol.NombreRol}");

        TempData["Mensaje"] = "Rol guardado. Los cambios de permisos se aplican en el próximo inicio de sesión de los usuarios.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Garantiza un permiso por cada módulo y niveles válidos (0 a 2).</summary>
    private static void Normalizar(RolViewModel m) =>
        m.Permisos = PermisoService.Modulos.Select(mod => new PermisoItemViewModel
        {
            Modulo = mod,
            Nivel = Math.Clamp(m.Permisos.FirstOrDefault(p => p.Modulo == mod)?.Nivel ?? 0, 0, 2)
        }).ToList();
}
