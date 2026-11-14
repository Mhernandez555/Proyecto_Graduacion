using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

/// <summary>
/// Gestión de tareas y registro de tiempo (RF-004). Acceso total (Administrador, Soporte TI): crea, edita y ve todo.
/// Acceso parcial (Empleado): solo ve sus tareas, registra tiempo, cambia el estado y escribe comentarios.
/// </summary>
[RequiereModulo("TAREAS", 1)]
public class TareasController(ControlexDbContext db, BitacoraService bitacora) : Controller
{
    private const int Limite = 200;
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Total => int.TryParse(User.FindFirst("perm:TAREAS")?.Value, out var n) && n >= 2;
    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);
    private static DateOnly? ADate(DateTime? d) => d is null ? (DateOnly?)null : DateOnly.FromDateTime(d.Value);

    private Task<bool> EstaAsignado(int idTarea, int idUsuario) =>
        db.TareaUsuarios.AnyAsync(a => a.IdTarea == idTarea && a.IdUsuario == idUsuario);

    private IActionResult Fallo(int id, string mensaje)
    {
        TempData["Error"] = mensaje;
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- Ver tareas ----------
    public async Task<IActionResult> Index(int? idProyecto, int? idEstado, int? idUsuario)
    {
        var yo = IdActual;
        var q = db.Tareas.AsNoTracking().AsQueryable();
        if (!Total)
        {
            q = q.Where(t => db.TareaUsuarios.Any(a => a.IdTarea == t.IdTarea && a.IdUsuario == yo));
            idUsuario = null;
        }
        if (idProyecto is int p) q = q.Where(t => t.IdProyecto == p);
        if (idEstado is int e) q = q.Where(t => t.IdEstado == e);
        if (idUsuario is int u) q = q.Where(t => db.TareaUsuarios.Any(a => a.IdTarea == t.IdTarea && a.IdUsuario == u));

        var tareas = await q.OrderByDescending(t => t.IdTarea).Take(Limite + 1).Select(t => new
        {
            t.IdTarea, t.Codigo, t.Nombre,
            Proyecto = t.Proyecto.Nombre, Categoria = t.Categoria.Nombre, Estado = t.EstadoActual.Nombre,
            t.FechaInicio, t.FechaFin, t.TiempoEstimado
        }).ToListAsync();
        var hayMas = tareas.Count > Limite;
        if (hayMas) tareas = tareas.Take(Limite).ToList();

        var ids = tareas.Select(t => t.IdTarea).ToList();
        var asignados = (await db.TareaUsuarios.AsNoTracking().Where(a => ids.Contains(a.IdTarea))
            .Select(a => new { a.IdTarea, Nombre = a.Usuario.Nombre + " " + a.Usuario.Apellido1 }).ToListAsync())
            .ToLookup(a => a.IdTarea, a => a.Nombre);
        var horas = await db.RegistrosTiempo.AsNoTracking().Where(r => ids.Contains(r.IdTarea))
            .GroupBy(r => r.IdTarea).Select(g => new { IdTarea = g.Key, Horas = g.Sum(r => r.Horas) })
            .ToDictionaryAsync(x => x.IdTarea, x => x.Horas);

        var filas = tareas.Select(t => new TareaListado(t.IdTarea, t.Codigo, t.Nombre, t.Proyecto, t.Categoria,
            string.Join(", ", asignados[t.IdTarea]), t.Estado, t.FechaInicio, t.FechaFin,
            t.TiempoEstimado, horas.GetValueOrDefault(t.IdTarea, 0m))).ToList();

        await CargarFiltros(idProyecto, idEstado, idUsuario);
        return View(new TareasIndexViewModel { Filas = filas, HayMas = hayMas, Total = Total, Limite = Limite });
    }

    // ---------- Consultar / registrar tiempo y estado ----------
    public async Task<IActionResult> Detalle(int id)
    {
        var yo = IdActual;
        var asignado = await EstaAsignado(id, yo);
        if (!Total && !asignado) return Forbid();

        var t = await db.Tareas.AsNoTracking().Where(x => x.IdTarea == id).Select(x => new
        {
            x.IdTarea, x.Codigo, x.Nombre, x.Descripcion,
            Proyecto = x.Proyecto.Codigo + " · " + x.Proyecto.Nombre, Categoria = x.Categoria.Nombre,
            x.IdEstado, Estado = x.EstadoActual.Nombre, x.FechaInicio, x.FechaFin, x.TiempoEstimado, x.Comentarios
        }).FirstOrDefaultAsync();
        if (t is null) return NotFound();

        var usuarios = await db.TareaUsuarios.AsNoTracking().Where(a => a.IdTarea == id)
            .Select(a => new { a.IdUsuario, Nombre = a.Usuario.Nombre + " " + a.Usuario.Apellido1 }).ToListAsync();
        var nombres = usuarios.ToDictionary(u => u.IdUsuario, u => u.Nombre);
        var registros = await db.RegistrosTiempo.AsNoTracking().Where(r => r.IdTarea == id)
            .OrderByDescending(r => r.FechaRegistro).ThenByDescending(r => r.IdRegistro).ToListAsync();
        var hoy = DateTime.Today;

        var vm = new TareaDetalleViewModel
        {
            IdTarea = t.IdTarea, Codigo = t.Codigo, Nombre = t.Nombre, Descripcion = t.Descripcion, Proyecto = t.Proyecto,
            Categoria = t.Categoria, Estado = t.Estado, FechaInicio = t.FechaInicio, FechaFin = t.FechaFin,
            Estimado = t.TiempoEstimado, Comentarios = t.Comentarios,
            Asignados = usuarios.Select(u => new AsignadoItem(u.IdUsuario, u.Nombre,
                registros.Where(r => r.IdUsuario == u.IdUsuario).Sum(r => r.Horas))).ToList(),
            Registros = registros.Select(r => new RegistroItem(r.IdRegistro, r.FechaRegistro, r.IdUsuario,
                nombres.GetValueOrDefault(r.IdUsuario, "—"), r.Horas, r.CostoHora, r.Comentario,
                Total || (r.IdUsuario == yo && r.FechaCreacion.Date == hoy))).ToList(),
            PuedeEditar = Total, VerCostos = Total, ElegirUsuario = Total, IdUsuarioActual = yo, EsAsignado = asignado
        };
        ViewBag.Estados = new SelectList(
            await db.Estados.AsNoTracking().Where(e => e.TipoEstado == "TAREA").OrderBy(e => e.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", t.IdEstado);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarEstado(int id, int idEstado, string? comentarios)
    {
        var t = await db.Tareas.FindAsync(id);
        if (t is null) return NotFound();
        if (!Total && !await EstaAsignado(id, IdActual)) return Forbid();

        var nuevo = await db.Estados.FirstOrDefaultAsync(e => e.IdEstado == idEstado && e.TipoEstado == "TAREA");
        if (nuevo is null) return Fallo(id, "Seleccione un estado válido.");
        if (comentarios is { Length: > 1000 }) return Fallo(id, "Los comentarios no pueden superar 1000 caracteres.");

        var anterior = await db.Estados.Where(e => e.IdEstado == t.IdEstado).Select(e => e.Nombre).FirstAsync();
        var cambioEstado = t.IdEstado != nuevo.IdEstado;
        t.IdEstado = nuevo.IdEstado;
        t.Comentarios = comentarios?.Trim();
        // Al pasar a "En proceso" o "Completada" se completan las fechas que estén vacías
        if (nuevo.Nombre == "En proceso" && t.FechaInicio is null) t.FechaInicio = Hoy;
        if (nuevo.Nombre == "Completada" && t.FechaFin is null) t.FechaFin = Hoy;
        await db.SaveChangesAsync();

        if (cambioEstado)
            await bitacora.MovimientoAsync(IdActual, "TAREAS", "CAMBIO DE ESTADO", $"Tarea {t.Codigo}: {anterior} → {nuevo.Nombre}");
        else
            await bitacora.MovimientoAsync(IdActual, "TAREAS", "MODIFICACIÓN", $"Tarea {t.Codigo}: comentarios actualizados");

        TempData["Mensaje"] = "Tarea actualizada.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarTiempo(int id, int? idUsuario, DateTime? fecha, decimal? horas, string? comentario)
    {
        var codigo = await db.Tareas.Where(t => t.IdTarea == id).Select(t => t.Codigo).FirstOrDefaultAsync();
        if (codigo is null) return NotFound();
        var yo = IdActual;
        if (!Total && !await EstaAsignado(id, yo)) return Forbid();

        var uid = Total ? (idUsuario ?? 0) : yo;
        if (uid == 0) return Fallo(id, "Seleccione el usuario.");
        if (!await EstaAsignado(id, uid)) return Fallo(id, "El usuario debe estar asignado a la tarea para registrar tiempo.");
        if (fecha is null) return Fallo(id, "Ingrese la fecha.");
        var dia = DateOnly.FromDateTime(fecha.Value);
        if (dia > Hoy) return Fallo(id, "La fecha no puede ser futura.");
        if (horas is null || horas.Value <= 0) return Fallo(id, "Ingrese las horas trabajadas (mayor que cero).");
        if (horas.Value > 24) return Fallo(id, "No puede registrar más de 24 horas en un día.");
        if (comentario is { Length: > 500 }) return Fallo(id, "El comentario no puede superar 500 caracteres.");

        var h = Math.Round(horas.Value, 2);
        var yaEseDia = await db.RegistrosTiempo.Where(r => r.IdUsuario == uid && r.FechaRegistro == dia)
            .SumAsync(r => (decimal?)r.Horas) ?? 0m;
        if (yaEseDia + h > 24)
            return Fallo(id, $"Ese día ya tiene {yaEseDia:0.##} h registradas entre todas las tareas; el máximo es 24 h.");

        var usuario = await db.Usuarios.FindAsync(uid);
        db.RegistrosTiempo.Add(new RegistroTiempo
        {
            IdTarea = id, IdUsuario = uid, FechaRegistro = dia, Horas = h,
            CostoHora = usuario!.CostoHora,   // costo por hora vigente en este momento
            Comentario = comentario?.Trim(), FechaCreacion = DateTime.Now
        });
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(yo, "TAREAS", "REGISTRO DE TIEMPO",
            $"Tarea {codigo}: {h:0.##} h el {dia:dd/MM/yyyy} ({usuario.NombreUsuario})");

        TempData["Mensaje"] = usuario.CostoHora == 0
            ? "Tiempo registrado. Aviso: el costo por hora de ese usuario es ₡0; defínalo en Usuarios para que el costo del proyecto sea real."
            : "Tiempo registrado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarRegistro(int id, int idRegistro)
    {
        var r = await db.RegistrosTiempo.FirstOrDefaultAsync(x => x.IdRegistro == idRegistro && x.IdTarea == id);
        if (r is null) return NotFound();
        // El acceso total elimina cualquiera; los demás, solo el propio y registrado hoy
        var propio = r.IdUsuario == IdActual && r.FechaCreacion.Date == DateTime.Today;
        if (!Total && !propio) return Forbid();

        var codigo = await db.Tareas.Where(t => t.IdTarea == id).Select(t => t.Codigo).FirstAsync();
        db.RegistrosTiempo.Remove(r);
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "TAREAS", "ELIMINACIÓN",
            $"Tarea {codigo}: registro de {r.Horas:0.##} h del {r.FechaRegistro:dd/MM/yyyy}");
        TempData["Mensaje"] = "Registro de tiempo eliminado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- Crear y editar (acceso total) ----------
    /// <summary>Si se llega desde un proyecto (idProyecto), la tarea queda ligada a ese proyecto de forma automática.</summary>
    [RequiereModulo("TAREAS", 2), HttpGet]
    public async Task<IActionResult> Crear(int? idProyecto)
    {
        var pendiente = await db.Estados.Where(e => e.TipoEstado == "TAREA" && e.Nombre == "Pendiente")
            .Select(e => e.IdEstado).FirstOrDefaultAsync();
        var m = new TareaViewModel { IdEstado = pendiente };

        if (idProyecto is int ip)
        {
            var estado = await db.Proyectos.Where(p => p.IdProyecto == ip).Select(p => p.EstadoActual.Nombre).FirstOrDefaultAsync();
            if (estado is null) TempData["Error"] = "El proyecto indicado no existe.";
            else if (estado is "Finalizado" or "Cancelado") TempData["Error"] = "El proyecto está finalizado o cancelado y no admite tareas nuevas.";
            else m.IdProyecto = ip;
        }
        await PrepararFormulario(m);
        return View("Formulario", m);
    }

    [RequiereModulo("TAREAS", 2), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(TareaViewModel m)
    {
        await Validar(m, null, new List<int>());
        if (!ModelState.IsValid) { await PrepararFormulario(m); return View("Formulario", m); }

        await using var tx = await db.Database.BeginTransactionAsync();
        var t = new Tarea
        {
            Nombre = m.Nombre.Trim(), Descripcion = m.Descripcion?.Trim(), IdProyecto = m.IdProyecto, IdCategoria = m.IdCategoria,
            IdEstado = m.IdEstado, FechaInicio = ADate(m.FechaInicio), FechaFin = ADate(m.FechaFin),
            TiempoEstimado = m.TiempoEstimado, Comentarios = m.Comentarios?.Trim(), FechaCreacion = DateTime.Now
        };
        db.Tareas.Add(t);
        await db.SaveChangesAsync();
        foreach (var uid in m.IdsUsuarios.Distinct())
            db.TareaUsuarios.Add(new TareaUsuario { IdTarea = t.IdTarea, IdUsuario = uid, FechaAsignacion = DateTime.Now });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        await bitacora.MovimientoAsync(IdActual, "TAREAS", "INSERCIÓN", $"Tarea creada: {t.Codigo} {t.Nombre}");
        TempData["Mensaje"] = $"Tarea creada ({t.Codigo}).";
        return RedirectToAction(nameof(Detalle), new { id = t.IdTarea });
    }

    [RequiereModulo("TAREAS", 2), HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var t = await db.Tareas.AsNoTracking().FirstOrDefaultAsync(x => x.IdTarea == id);
        if (t is null) return NotFound();
        var m = new TareaViewModel
        {
            IdTarea = t.IdTarea, Codigo = t.Codigo, Nombre = t.Nombre, Descripcion = t.Descripcion, IdProyecto = t.IdProyecto,
            IdCategoria = t.IdCategoria, IdEstado = t.IdEstado,
            FechaInicio = t.FechaInicio?.ToDateTime(TimeOnly.MinValue), FechaFin = t.FechaFin?.ToDateTime(TimeOnly.MinValue),
            TiempoEstimado = t.TiempoEstimado, Comentarios = t.Comentarios,
            IdsUsuarios = await db.TareaUsuarios.Where(a => a.IdTarea == id).Select(a => a.IdUsuario).ToListAsync()
        };
        await PrepararFormulario(m);
        return View("Formulario", m);
    }

    [RequiereModulo("TAREAS", 2), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, TareaViewModel m)
    {
        var t = await db.Tareas.FindAsync(id);
        if (t is null) return NotFound();
        m.IdTarea = id;
        m.Codigo = t.Codigo;

        var actuales = await db.TareaUsuarios.Where(a => a.IdTarea == id).ToListAsync();
        var yaIds = actuales.Select(a => a.IdUsuario).ToList();
        await Validar(m, t.IdProyecto, yaIds);

        // No se puede quitar a quien ya registró tiempo (el costo del proyecto depende de esos registros)
        var seleccion = m.IdsUsuarios.Distinct().ToList();
        var quitar = yaIds.Except(seleccion).ToList();
        if (quitar.Count > 0)
        {
            var conTiempo = await db.RegistrosTiempo.Where(r => r.IdTarea == id && quitar.Contains(r.IdUsuario))
                .Select(r => r.IdUsuario).Distinct().ToListAsync();
            if (conTiempo.Count > 0)
            {
                var nombres = await db.Usuarios.Where(u => conTiempo.Contains(u.IdUsuario))
                    .Select(u => u.Nombre + " " + u.Apellido1).ToListAsync();
                ModelState.AddModelError(nameof(m.IdsUsuarios),
                    $"No puede quitar a {string.Join(", ", nombres)}: ya registró tiempo en esta tarea.");
            }
        }
        if (!ModelState.IsValid) { await PrepararFormulario(m); return View("Formulario", m); }

        await using var tx = await db.Database.BeginTransactionAsync();
        t.Nombre = m.Nombre.Trim(); t.Descripcion = m.Descripcion?.Trim(); t.IdProyecto = m.IdProyecto; t.IdCategoria = m.IdCategoria;
        t.IdEstado = m.IdEstado; t.FechaInicio = ADate(m.FechaInicio); t.FechaFin = ADate(m.FechaFin);
        t.TiempoEstimado = m.TiempoEstimado; t.Comentarios = m.Comentarios?.Trim();
        db.TareaUsuarios.RemoveRange(actuales.Where(a => quitar.Contains(a.IdUsuario)));
        foreach (var uid in seleccion.Except(yaIds))
            db.TareaUsuarios.Add(new TareaUsuario { IdTarea = id, IdUsuario = uid, FechaAsignacion = DateTime.Now });
        await db.SaveChangesAsync();
        await tx.CommitAsync();

        await bitacora.MovimientoAsync(IdActual, "TAREAS", "MODIFICACIÓN", $"Tarea modificada: {t.Codigo} {t.Nombre}");
        TempData["Mensaje"] = "Tarea actualizada.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- apoyo ----------
    private async Task Validar(TareaViewModel m, int? proyectoActual, List<int> yaAsignados)
    {
        if (m.FechaInicio is DateTime i && m.FechaFin is DateTime f && f.Date < i.Date)
            ModelState.AddModelError(nameof(m.FechaFin), "La fecha de finalización no puede ser anterior a la de inicio.");

        if (m.IdProyecto > 0)
        {
            var estado = await db.Proyectos.Where(p => p.IdProyecto == m.IdProyecto)
                .Select(p => p.EstadoActual.Nombre).FirstOrDefaultAsync();
            if (estado is null)
                ModelState.AddModelError(nameof(m.IdProyecto), "Seleccione un proyecto válido.");
            else if (m.IdProyecto != proyectoActual && estado is "Finalizado" or "Cancelado")
                ModelState.AddModelError(nameof(m.IdProyecto), "No se pueden agregar tareas a un proyecto finalizado o cancelado.");
        }
        if (m.IdCategoria > 0 && !await db.Categorias.AnyAsync(c => c.IdCategoria == m.IdCategoria))
            ModelState.AddModelError(nameof(m.IdCategoria), "Seleccione una categoría válida.");
        if (m.IdEstado > 0 && !await db.Estados.AnyAsync(e => e.IdEstado == m.IdEstado && e.TipoEstado == "TAREA"))
            ModelState.AddModelError(nameof(m.IdEstado), "Seleccione un estado válido.");

        var ids = m.IdsUsuarios.Distinct().ToList();
        if (ids.Count == 0)
        {
            ModelState.AddModelError(nameof(m.IdsUsuarios), "Asigne la tarea al menos a un usuario.");
            return;
        }
        if (await db.Usuarios.CountAsync(u => ids.Contains(u.IdUsuario)) != ids.Count)
            ModelState.AddModelError(nameof(m.IdsUsuarios), "Hay usuarios que no existen.");
        var nuevos = ids.Except(yaAsignados).ToList();
        if (nuevos.Count > 0 && await db.Usuarios.CountAsync(u => nuevos.Contains(u.IdUsuario) && u.Estado) != nuevos.Count)
            ModelState.AddModelError(nameof(m.IdsUsuarios), "Solo se puede asignar a usuarios activos.");
    }

    private async Task PrepararFormulario(TareaViewModel m)
    {
        var idProyecto = m.IdProyecto;
        var idCategoria = m.IdCategoria;
        var asignados = m.IdsUsuarios.ToList();

        ViewBag.Proyectos = new SelectList(
            await db.Proyectos.AsNoTracking()
                .Where(p => (p.EstadoActual.Nombre != "Finalizado" && p.EstadoActual.Nombre != "Cancelado") || p.IdProyecto == idProyecto)
                .OrderBy(p => p.Codigo)
                .Select(p => new { p.IdProyecto, Texto = p.Codigo + " · " + p.Nombre }).ToListAsync(),
            "IdProyecto", "Texto", m.IdProyecto);
        ViewBag.Categorias = new SelectList(
            await db.Categorias.AsNoTracking().Where(c => c.Estado || c.IdCategoria == idCategoria).OrderBy(c => c.Nombre).ToListAsync(),
            "IdCategoria", "Nombre", m.IdCategoria);
        ViewBag.Estados = new SelectList(
            await db.Estados.AsNoTracking().Where(e => e.TipoEstado == "TAREA").OrderBy(e => e.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", m.IdEstado);
        ViewBag.Usuarios = await db.Usuarios.AsNoTracking()
            .Where(u => u.Estado || asignados.Contains(u.IdUsuario)).OrderBy(u => u.Nombre)
            .Select(u => new OpcionUsuario(u.IdUsuario, u.Nombre + " " + u.Apellido1 + (u.Estado ? "" : " (inactivo)")))
            .ToListAsync();
    }

    private async Task CargarFiltros(int? idProyecto, int? idEstado, int? idUsuario)
    {
        ViewBag.Proyectos = new SelectList(
            await db.Proyectos.AsNoTracking().OrderBy(p => p.Codigo)
                .Select(p => new { p.IdProyecto, Texto = p.Codigo + " · " + p.Nombre }).ToListAsync(),
            "IdProyecto", "Texto", idProyecto);
        ViewBag.Estados = new SelectList(
            await db.Estados.AsNoTracking().Where(e => e.TipoEstado == "TAREA").OrderBy(e => e.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", idEstado);
        if (Total)
            ViewBag.Usuarios = new SelectList(
                await db.Usuarios.AsNoTracking().OrderBy(u => u.Nombre)
                    .Select(u => new { u.IdUsuario, Texto = u.Nombre + " " + u.Apellido1 }).ToListAsync(),
                "IdUsuario", "Texto", idUsuario);
    }
}
