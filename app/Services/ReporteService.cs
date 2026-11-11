using Controlex.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Services;

/// <summary>Consultas de los reportes. Las usan tanto la pantalla como la exportación a PDF.</summary>
public class ReporteService(ControlexDbContext db)
{
    // ---------- RF-005: proyectos y rentabilidad ----------
    public async Task<List<FilaRentabilidad>> RentabilidadAsync(RentabilidadFiltro f)
    {
        var q = db.RentabilidadProyectos.AsNoTracking().AsQueryable();
        if (f.IdProyecto is int id) q = q.Where(x => x.IdProyecto == id);
        // El período filtra por la fecha de inicio del proyecto
        if (f.Desde is DateTime d) { var desde = DateOnly.FromDateTime(d); q = q.Where(x => x.FechaInicio >= desde); }
        if (f.Hasta is DateTime h) { var hasta = DateOnly.FromDateTime(h); q = q.Where(x => x.FechaInicio <= hasta); }

        var datos = await q.OrderBy(x => x.Codigo).ToListAsync();
        return datos.Select(x => new FilaRentabilidad(x.Codigo, x.Proyecto, x.NombreCliente, x.Estado,
            x.FechaInicio, x.FechaFin, x.PrecioVenta, x.CostoAcumulado, x.Rentabilidad)).ToList();
    }

    public async Task<string> DescribirAsync(RentabilidadFiltro f)
    {
        var proyecto = f.IdProyecto is int id
            ? (await db.Proyectos.Where(p => p.IdProyecto == id).Select(p => p.Codigo + " " + p.Nombre).FirstOrDefaultAsync() ?? "—")
            : "todos";
        return $"Proyecto: {proyecto}  |  Período (fecha de inicio): {Periodo(f.Desde, f.Hasta)}";
    }

    // ---------- RF-024: eficiencia de tareas y pendientes ----------
    public async Task<List<FilaTarea>> TareasAsync(TareasFiltro f)
    {
        var q = db.Tareas.AsNoTracking().AsQueryable();
        if (f.IdProyecto is int p) q = q.Where(t => t.IdProyecto == p);
        if (f.IdEstado is int e) q = q.Where(t => t.IdEstado == e);
        if (f.IdUsuario is int u)
            q = q.Where(t => db.TareaUsuarios.Any(a => a.IdTarea == t.IdTarea && a.IdUsuario == u));
        // El período filtra por la fecha de finalización de la tarea (o la de inicio si no tiene)
        if (f.Desde is DateTime d) { var desde = DateOnly.FromDateTime(d); q = q.Where(t => (t.FechaFin ?? t.FechaInicio) >= desde); }
        if (f.Hasta is DateTime h) { var hasta = DateOnly.FromDateTime(h); q = q.Where(t => (t.FechaFin ?? t.FechaInicio) <= hasta); }
        if (f.Tipo == "pendientes") q = q.Where(t => t.EstadoActual.Nombre != "Completada");

        var tareas = await q.Select(t => new
        {
            t.IdTarea, t.Codigo, t.Nombre,
            Proyecto = t.Proyecto.Nombre, Categoria = t.Categoria.Nombre, Estado = t.EstadoActual.Nombre,
            t.FechaInicio, t.FechaFin, t.TiempoEstimado
        }).ToListAsync();

        var ids = tareas.Select(t => t.IdTarea).ToList();
        var asignados = (await db.TareaUsuarios.AsNoTracking().Where(a => ids.Contains(a.IdTarea))
            .Select(a => new { a.IdTarea, Nombre = a.Usuario.Nombre + " " + a.Usuario.Apellido1 }).ToListAsync())
            .ToLookup(a => a.IdTarea, a => a.Nombre);
        var horas = await db.RegistrosTiempo.AsNoTracking().Where(r => ids.Contains(r.IdTarea))
            .GroupBy(r => r.IdTarea).Select(g => new { IdTarea = g.Key, Horas = g.Sum(r => r.Horas) })
            .ToDictionaryAsync(x => x.IdTarea, x => x.Horas);

        var filas = tareas.Select(t => new FilaTarea(t.Codigo, t.Nombre, t.Proyecto, t.Categoria,
            string.Join(", ", asignados[t.IdTarea]), t.Estado, t.FechaInicio, t.FechaFin,
            t.TiempoEstimado, horas.GetValueOrDefault(t.IdTarea, 0m), t.Estado != "Completada"));

        return (f.Tipo == "pendientes"
            ? filas.OrderBy(x => x.FechaFin is null).ThenBy(x => x.FechaFin).ThenBy(x => x.Codigo)
            : filas.OrderBy(x => x.Proyecto).ThenBy(x => x.Codigo)).ToList();
    }

    public async Task<string> DescribirAsync(TareasFiltro f)
    {
        var tipo = f.Tipo == "pendientes" ? "Tareas pendientes" : "Eficiencia de tareas";
        var proyecto = f.IdProyecto is int p
            ? (await db.Proyectos.Where(x => x.IdProyecto == p).Select(x => x.Codigo + " " + x.Nombre).FirstOrDefaultAsync() ?? "—")
            : "todos";
        var usuario = f.IdUsuario is int u
            ? (await db.Usuarios.Where(x => x.IdUsuario == u).Select(x => x.Nombre + " " + x.Apellido1).FirstOrDefaultAsync() ?? "—")
            : "todos";
        var estado = f.IdEstado is int e
            ? (await db.Estados.Where(x => x.IdEstado == e).Select(x => x.Nombre).FirstOrDefaultAsync() ?? "—")
            : "todos";
        return $"{tipo}  |  Proyecto: {proyecto}  |  Usuario: {usuario}  |  Estado: {estado}  |  Período: {Periodo(f.Desde, f.Hasta)}";
    }

    private static string Periodo(DateTime? desde, DateTime? hasta) => (desde, hasta) switch
    {
        (not null, not null) => $"{desde:dd/MM/yyyy} al {hasta:dd/MM/yyyy}",
        (not null, null) => $"desde {desde:dd/MM/yyyy}",
        (null, not null) => $"hasta {hasta:dd/MM/yyyy}",
        _ => "sin límite"
    };
}
