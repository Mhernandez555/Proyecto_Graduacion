using System.Security.Claims;
using Controlex.Web.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

public class DashboardController(ControlexDbContext contexto) : Controller
{
    public async Task<IActionResult> Index()
    {
        var idActual = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var hoy = DateOnly.FromDateTime(DateTime.Today);
        var modeloInicio = new InicioViewModel
        {
            VeEquipo = int.TryParse(User.FindFirst("perm:PROYECTOS")?.Value, out var nivel) && nivel >= 2
        };

        // Tareas asignadas al usuario
        var tareasAsignadas = contexto.Tareas.AsNoTracking()
            .Where(tarea => contexto.TareaUsuarios.Any(asignacion =>
                asignacion.IdTarea == tarea.IdTarea && asignacion.IdUsuario == idActual));
        modeloInicio.MisPendientes = await tareasAsignadas.CountAsync(tarea => tarea.EstadoActual.Nombre == "Pendiente");
        modeloInicio.MisEnProceso = await tareasAsignadas.CountAsync(tarea => tarea.EstadoActual.Nombre == "En proceso");
        modeloInicio.MisVencidas = await tareasAsignadas.CountAsync(tarea =>
            tarea.EstadoActual.Nombre != "Completada" && tarea.FechaFin != null && tarea.FechaFin < hoy);
        modeloInicio.ProximasTareas = await tareasAsignadas.Where(tarea => tarea.EstadoActual.Nombre != "Completada")
            .OrderBy(tarea => tarea.FechaFin == null).ThenBy(tarea => tarea.FechaFin)
            .Take(5)
            .Select(tarea => new TareaResumen(tarea.Codigo, tarea.Nombre, tarea.Proyecto.Nombre,
                tarea.EstadoActual.Nombre, tarea.FechaFin))
            .ToListAsync();

        // Vista del equipo
        if (modeloInicio.VeEquipo)
        {
            modeloInicio.ProyectosEnEjecucion = await contexto.Proyectos.CountAsync(proyecto =>
                proyecto.EstadoActual.Nombre == "En ejecución");
            modeloInicio.TareasPendientesEquipo = await contexto.Tareas.CountAsync(tarea =>
                tarea.EstadoActual.Nombre != "Completada");
            modeloInicio.ProyectosActivos = await contexto.Proyectos.AsNoTracking()
                .Where(proyecto => proyecto.EstadoActual.Nombre == "En ejecución")
                .OrderByDescending(proyecto => proyecto.FechaCreacion).Take(5)
                .Select(proyecto => new ProyectoResumen(proyecto.Codigo, proyecto.Nombre,
                    proyecto.Cliente.NombreCliente, proyecto.EstadoActual.Nombre, proyecto.FechaFin))
                .ToListAsync();
        }
        return View(modeloInicio);
    }

    [AllowAnonymous]
    public IActionResult Error() => Content("Ocurrió un error inesperado. Intente nuevamente.");
}
