using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

[RequiereModulo("ROLES_PERMISOS", 1)]
public class RolesController(ControlexDbContext contexto, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var total = await contexto.Roles.CountAsync();
        var paginacion = Paginacion.Crear("Roles", nameof(Index), pagina, total);
        var roles = await contexto.Roles.AsNoTracking().OrderBy(rol => rol.NombreRol).ThenBy(rol => rol.IdRol)
            .Skip((paginacion.Pagina - 1) * paginacion.Tam).Take(paginacion.Tam).ToListAsync();
        return View(new RolesIndexViewModel { Filas = roles, Paginacion = paginacion });
    }

    [HttpGet]
    public IActionResult Crear()
    {
        var modeloRol = new RolViewModel();
        Normalizar(modeloRol);
        return View("Formulario", modeloRol);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(RolViewModel modeloRol)
    {
        Normalizar(modeloRol);
        if (await contexto.Roles.AnyAsync(rol => rol.NombreRol == modeloRol.NombreRol))
            ModelState.AddModelError(nameof(modeloRol.NombreRol), "Ya existe un rol con ese nombre.");
        if (!ModelState.IsValid) return View("Formulario", modeloRol);

        var rol = new Rol { NombreRol = modeloRol.NombreRol.Trim(), Descripcion = modeloRol.Descripcion?.Trim(),
            EsAdministrador = modeloRol.EsAdministrador, Estado = modeloRol.Estado };
        contexto.Roles.Add(rol);
        await contexto.SaveChangesAsync();
        foreach (var permiso in modeloRol.Permisos)
            contexto.Permisos.Add(new Permiso { IdRol = rol.IdRol, Modulo = permiso.Modulo, NivelAcceso = (byte)permiso.Nivel });
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "ROLES_PERMISOS", "INSERCIÓN", $"Rol creado: {rol.NombreRol}");

        TempData["Mensaje"] = "Rol creado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var rol = await contexto.Roles.FindAsync(id);
        if (rol is null) return NotFound();
        var nivelesPorModulo = await contexto.Permisos.Where(permiso => permiso.IdRol == id)
            .ToDictionaryAsync(permiso => permiso.Modulo, permiso => (int)permiso.NivelAcceso);
        var modeloRol = new RolViewModel
        {
            IdRol = rol.IdRol, NombreRol = rol.NombreRol, Descripcion = rol.Descripcion,
            EsAdministrador = rol.EsAdministrador, Estado = rol.Estado,
            Permisos = PermisoService.Modulos.Select(modulo =>
                new PermisoItemViewModel { Modulo = modulo, Nivel = nivelesPorModulo.GetValueOrDefault(modulo, 0) }).ToList()
        };
        return View("Formulario", modeloRol);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, RolViewModel modeloRol)
    {
        modeloRol.IdRol = id;
        Normalizar(modeloRol);
        var rol = await contexto.Roles.FindAsync(id);
        if (rol is null) return NotFound();

        if (await contexto.Roles.AnyAsync(otroRol => otroRol.NombreRol == modeloRol.NombreRol && otroRol.IdRol != id))
            ModelState.AddModelError(nameof(modeloRol.NombreRol), "Ya existe un rol con ese nombre.");
        // Siempre debe quedar al menos un rol administrador activo
        var pierdeAdministrador = rol.EsAdministrador && (!modeloRol.EsAdministrador || !modeloRol.Estado);
        if (pierdeAdministrador && !await contexto.Roles.AnyAsync(otroRol =>
                otroRol.IdRol != id && otroRol.EsAdministrador && otroRol.Estado))
            ModelState.AddModelError("", "Debe existir al menos un rol administrador activo.");
        if (!ModelState.IsValid) return View("Formulario", modeloRol);

        rol.NombreRol = modeloRol.NombreRol.Trim(); rol.Descripcion = modeloRol.Descripcion?.Trim();
        rol.EsAdministrador = modeloRol.EsAdministrador; rol.Estado = modeloRol.Estado;

        var permisosExistentes = await contexto.Permisos.Where(permiso => permiso.IdRol == id).ToListAsync();
        foreach (var permiso in modeloRol.Permisos)
        {
            var permisoExistente = permisosExistentes.FirstOrDefault(registro => registro.Modulo == permiso.Modulo);
            if (permisoExistente is null)
                contexto.Permisos.Add(new Permiso { IdRol = id, Modulo = permiso.Modulo, NivelAcceso = (byte)permiso.Nivel });
            else permisoExistente.NivelAcceso = (byte)permiso.Nivel;
        }
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "ROLES_PERMISOS", "MODIFICACIÓN", $"Rol modificado: {rol.NombreRol}");

        TempData["Mensaje"] = "Rol guardado. Los cambios de permisos se aplican en el próximo inicio de sesión de los usuarios.";
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Garantiza un permiso por cada módulo y niveles válidos (0 a 2).</summary>
    private static void Normalizar(RolViewModel modeloRol) =>
        modeloRol.Permisos = PermisoService.Modulos.Select(modulo => new PermisoItemViewModel
        {
            Modulo = modulo,
            Nivel = Math.Clamp(modeloRol.Permisos.FirstOrDefault(permiso => permiso.Modulo == modulo)?.Nivel ?? 0, 0, 2)
        }).ToList();
}
