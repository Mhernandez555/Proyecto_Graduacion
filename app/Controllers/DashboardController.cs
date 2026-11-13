using System.Security.Claims;
using Controlex.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

public class DashboardController(ControlexDbContext db) : Controller
{
    public async Task<IActionResult> Index()
    {
        var idActual = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var vm = new InicioViewModel
        {
            VeEquipo = int.TryParse(User.FindFirst("perm:PROYECTOS")?.Value, out var nivel) && nivel >= 2
        };

        // Tareas asignadas al usuario
        var mias = db.Tareas.AsNoTracking()
            .Where(t => db.TareaUsuarios.Any(a => a.IdTarea == t.IdTarea && a.IdUsuario == idActual));
        vm.MisPendientes = await mias.CountAsync(t => t.EstadoActual.Nombre == "Pendiente");
        vm.MisEnProceso = await mias.CountAsync(t => t.EstadoActual.Nombre == "En proceso");
        vm.MisVencidas = await mias.CountAsync(t => t.EstadoActual.Nombre != "Completada" && t.FechaFin != null && t.FechaFin < hoy);
        vm.ProximasTareas = await mias.Where(t => t.EstadoActual.Nombre != "Completada")
            .OrderBy(t => t.FechaFin == null).ThenBy(t => t.FechaFin)
            .Take(5)
            .Select(t => new TareaResumen(t.Codigo, t.Nombre, t.Proyecto.Nombre, t.EstadoActual.Nombre, t.FechaFin))
            .ToListAsync();

        // Vista del equipo
        if (vm.VeEquipo)
        {
            vm.ProyectosEnEjecucion = await db.Proyectos.CountAsync(p => p.EstadoActual.Nombre == "En ejecución");
            vm.TareasPendientesEquipo = await db.Tareas.CountAsync(t => t.EstadoActual.Nombre != "Completada");
            vm.ProyectosActivos = await db.Proyectos.AsNoTracking()
                .Where(p => p.EstadoActual.Nombre == "En ejecución")
                .OrderByDescending(p => p.FechaCreacion).Take(5)
                .Select(p => new ProyectoResumen(p.Codigo, p.Nombre, p.Cliente.NombreCliente, p.EstadoActual.Nombre, p.FechaFin))
                .ToListAsync();
        }
        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult Error() => Content("Ocurrió un error inesperado. Intente nuevamente.");
}
