using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

/// <summary>
/// Gestión de tareas y registro de tiempo (RF-004). Acceso total (Administrador, Soporte TI): crea, edita y ve todo.
/// Acceso parcial (Empleado): solo ve sus tareas, registra tiempo, cambia el estado y escribe comentarios.
/// </summary>
[RequiereModulo("TAREAS", 1)]
public class TareasController(ControlexDbContext contexto, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Total => int.TryParse(User.FindFirst("perm:TAREAS")?.Value, out var nivel) && nivel >= 2;
    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.Today);
    private static DateOnly? ADate(DateTime? fecha) => fecha is null ? (DateOnly?)null : DateOnly.FromDateTime(fecha.Value);

    private Task<bool> EstaAsignado(int idTarea, int idUsuario) =>
        contexto.TareaUsuarios.AnyAsync(asignacion => asignacion.IdTarea == idTarea && asignacion.IdUsuario == idUsuario);

    private IActionResult Fallo(int id, string mensaje)
    {
        TempData["Error"] = mensaje;
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- Ver tareas ----------
    public async Task<IActionResult> Index(int? idProyecto, int? idEstado, int? idUsuario, int pagina = 1)
    {
        var idUsuarioActual = IdActual;
        var consulta = contexto.Tareas.AsNoTracking().AsQueryable();
        if (!Total)
        {
            consulta = consulta.Where(tarea =>
                contexto.TareaUsuarios.Any(asignacion => asignacion.IdTarea == tarea.IdTarea &&
                    asignacion.IdUsuario == idUsuarioActual));
            idUsuario = null;
        }
        if (idProyecto is int idProyectoSeleccionado)
            consulta = consulta.Where(tarea => tarea.IdProyecto == idProyectoSeleccionado);
        if (idEstado is int idEstadoSeleccionado)
            consulta = consulta.Where(tarea => tarea.IdEstado == idEstadoSeleccionado);
        if (idUsuario is int idUsuarioSeleccionado)
            consulta = consulta.Where(tarea => contexto.TareaUsuarios.Any(asignacion =>
                asignacion.IdTarea == tarea.IdTarea && asignacion.IdUsuario == idUsuarioSeleccionado));

        var filtrosRuta = new RouteValueDictionary();
        if (idProyecto is int proyectoSeleccionado) filtrosRuta["idProyecto"] = proyectoSeleccionado;
        if (idEstado is int estadoSeleccionado) filtrosRuta["idEstado"] = estadoSeleccionado;
        if (idUsuario is int usuarioSeleccionado) filtrosRuta["idUsuario"] = usuarioSeleccionado;
        var paginacion = Paginacion.Crear("Tareas", nameof(Index), pagina, await consulta.CountAsync(), filtrosRuta);
        var tareas = await consulta.OrderByDescending(tarea => tarea.IdTarea)
            .Skip((paginacion.Pagina - 1) * paginacion.Tam).Take(paginacion.Tam).Select(tarea => new
        {
            tarea.IdTarea, tarea.Codigo, tarea.Nombre,
            Proyecto = tarea.Proyecto.Nombre, Categoria = tarea.Categoria.Nombre, Estado = tarea.EstadoActual.Nombre,
            tarea.FechaInicio, tarea.FechaFin, tarea.TiempoEstimado
        }).ToListAsync();

        var identificadoresTareas = tareas.Select(tarea => tarea.IdTarea).ToList();
        var asignados = (await contexto.TareaUsuarios.AsNoTracking()
            .Where(asignacion => identificadoresTareas.Contains(asignacion.IdTarea))
            .Select(asignacion => new
            {
                asignacion.IdTarea,
                Nombre = asignacion.Usuario.Nombre + " " + asignacion.Usuario.Apellido1
            }).ToListAsync())
            .ToLookup(asignacion => asignacion.IdTarea, asignacion => asignacion.Nombre);
        var horasPorTarea = await contexto.RegistrosTiempo.AsNoTracking()
            .Where(registro => identificadoresTareas.Contains(registro.IdTarea))
            .GroupBy(registro => registro.IdTarea)
            .Select(grupo => new { IdTarea = grupo.Key, Horas = grupo.Sum(registro => registro.Horas) })
            .ToDictionaryAsync(resumen => resumen.IdTarea, resumen => resumen.Horas);

        var filas = tareas.Select(tarea => new TareaListado(tarea.IdTarea, tarea.Codigo, tarea.Nombre,
            tarea.Proyecto, tarea.Categoria, string.Join(", ", asignados[tarea.IdTarea]), tarea.Estado,
            tarea.FechaInicio, tarea.FechaFin, tarea.TiempoEstimado,
            horasPorTarea.GetValueOrDefault(tarea.IdTarea, 0m))).ToList();

        await CargarFiltros(idProyecto, idEstado, idUsuario);
        return View(new TareasIndexViewModel { Filas = filas, Total = Total, Paginacion = paginacion });
    }

    // ---------- Consultar / registrar tiempo y estado ----------
    public async Task<IActionResult> Detalle(int id, int pagina = 1)
    {
        var idUsuarioActual = IdActual;
        var asignado = await EstaAsignado(id, idUsuarioActual);
        if (!Total && !asignado) return Forbid();

        var tarea = await contexto.Tareas.AsNoTracking().Where(registro => registro.IdTarea == id).Select(registro => new
        {
            registro.IdTarea, registro.Codigo, registro.Nombre, registro.Descripcion,
            Proyecto = registro.Proyecto.Codigo + " · " + registro.Proyecto.Nombre, Categoria = registro.Categoria.Nombre,
            registro.IdEstado, Estado = registro.EstadoActual.Nombre, registro.FechaInicio, registro.FechaFin,
            registro.TiempoEstimado, registro.Comentarios
        }).FirstOrDefaultAsync();
        if (tarea is null) return NotFound();

        var usuariosAsignados = await contexto.TareaUsuarios.AsNoTracking()
            .Where(asignacion => asignacion.IdTarea == id)
            .Select(asignacion => new
            {
                asignacion.IdUsuario,
                Nombre = asignacion.Usuario.Nombre + " " + asignacion.Usuario.Apellido1
            }).ToListAsync();
        var nombresUsuarios = usuariosAsignados.ToDictionary(usuario => usuario.IdUsuario, usuario => usuario.Nombre);
        var consultaRegistros = contexto.RegistrosTiempo.AsNoTracking().Where(registro => registro.IdTarea == id);
        var totalRegistros = await consultaRegistros.CountAsync();
        var paginacion = Paginacion.Crear("Tareas", nameof(Detalle), pagina, totalRegistros,
            new RouteValueDictionary { ["id"] = id });
        var horasPorUsuario = await consultaRegistros.GroupBy(registro => registro.IdUsuario)
            .Select(grupo => new { IdUsuario = grupo.Key, Horas = grupo.Sum(registro => registro.Horas) })
            .ToDictionaryAsync(resumen => resumen.IdUsuario, resumen => resumen.Horas);
        var registros = await consultaRegistros.OrderByDescending(registro => registro.FechaRegistro)
            .ThenByDescending(registro => registro.IdRegistro)
            .Skip((paginacion.Pagina - 1) * paginacion.Tam).Take(paginacion.Tam).ToListAsync();
        var hoy = DateTime.Today;

        var modeloVista = new TareaDetalleViewModel
        {
            IdTarea = tarea.IdTarea, Codigo = tarea.Codigo, Nombre = tarea.Nombre,
            Descripcion = tarea.Descripcion, Proyecto = tarea.Proyecto,
            Categoria = tarea.Categoria, Estado = tarea.Estado, FechaInicio = tarea.FechaInicio, FechaFin = tarea.FechaFin,
            Estimado = tarea.TiempoEstimado, Comentarios = tarea.Comentarios,
            Asignados = usuariosAsignados.Select(usuario => new AsignadoItem(usuario.IdUsuario, usuario.Nombre,
                horasPorUsuario.GetValueOrDefault(usuario.IdUsuario, 0m))).ToList(),
            Registros = registros.Select(registro => new RegistroItem(registro.IdRegistro, registro.FechaRegistro,
                registro.IdUsuario, nombresUsuarios.GetValueOrDefault(registro.IdUsuario, "—"),
                registro.Horas, registro.CostoHora, registro.Comentario,
                Total || (registro.IdUsuario == idUsuarioActual && registro.FechaCreacion.Date == hoy))).ToList(),
            PuedeEditar = Total, VerCostos = Total, ElegirUsuario = Total,
            IdUsuarioActual = idUsuarioActual, EsAsignado = asignado,
            TotalRegistrado = horasPorUsuario.Values.Sum(), Paginacion = paginacion
        };
        ViewBag.Estados = new SelectList(
            await contexto.Estados.AsNoTracking().Where(estado => estado.TipoEstado == "TAREA")
                .OrderBy(estado => estado.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", tarea.IdEstado);
        return View(modeloVista);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ActualizarEstado(int id, int idEstado, string? comentarios)
    {
        var tarea = await contexto.Tareas.FindAsync(id);
        if (tarea is null) return NotFound();
        if (!Total && !await EstaAsignado(id, IdActual)) return Forbid();

        var nuevoEstado = await contexto.Estados.FirstOrDefaultAsync(estado =>
            estado.IdEstado == idEstado && estado.TipoEstado == "TAREA");
        if (nuevoEstado is null) return Fallo(id, "Seleccione un estado válido.");
        if (comentarios is { Length: > 1000 }) return Fallo(id, "Los comentarios no pueden superar 1000 caracteres.");

        var estadoAnterior = await contexto.Estados.Where(estado => estado.IdEstado == tarea.IdEstado)
            .Select(estado => estado.Nombre).FirstAsync();
        var cambioEstado = tarea.IdEstado != nuevoEstado.IdEstado;
        tarea.IdEstado = nuevoEstado.IdEstado;
        tarea.Comentarios = comentarios?.Trim();
        // Al pasar a "En proceso" o "Completada" se completan las fechas que estén vacías
        if (nuevoEstado.Nombre == "En proceso" && tarea.FechaInicio is null) tarea.FechaInicio = Hoy;
        if (nuevoEstado.Nombre == "Completada" && tarea.FechaFin is null) tarea.FechaFin = Hoy;
        await contexto.SaveChangesAsync();

        if (cambioEstado)
            await bitacora.MovimientoAsync(IdActual, "TAREAS", "CAMBIO DE ESTADO",
                $"Tarea {tarea.Codigo}: {estadoAnterior} → {nuevoEstado.Nombre}");
        else
            await bitacora.MovimientoAsync(IdActual, "TAREAS", "MODIFICACIÓN",
                $"Tarea {tarea.Codigo}: comentarios actualizados");

        TempData["Mensaje"] = "Tarea actualizada.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> RegistrarTiempo(int id, int? idUsuario, DateTime? fecha, decimal? horas, string? comentario)
    {
        var codigo = await contexto.Tareas.Where(tarea => tarea.IdTarea == id)
            .Select(tarea => tarea.Codigo).FirstOrDefaultAsync();
        if (codigo is null) return NotFound();
        var idUsuarioActual = IdActual;
        if (!Total && !await EstaAsignado(id, idUsuarioActual)) return Forbid();

        var idUsuarioSeleccionado = Total ? (idUsuario ?? 0) : idUsuarioActual;
        if (idUsuarioSeleccionado == 0) return Fallo(id, "Seleccione el usuario.");
        if (!await EstaAsignado(id, idUsuarioSeleccionado))
            return Fallo(id, "El usuario debe estar asignado a la tarea para registrar tiempo.");
        if (fecha is null) return Fallo(id, "Ingrese la fecha.");
        var diaRegistro = DateOnly.FromDateTime(fecha.Value);
        if (diaRegistro > Hoy) return Fallo(id, "La fecha no puede ser futura.");
        if (horas is null || horas.Value <= 0) return Fallo(id, "Ingrese las horas trabajadas (mayor que cero).");
        if (horas.Value > 24) return Fallo(id, "No puede registrar más de 24 horas en un día.");
        if (comentario is { Length: > 500 }) return Fallo(id, "El comentario no puede superar 500 caracteres.");

        var horasRedondeadas = Math.Round(horas.Value, 2);
        var horasRegistradasEseDia = await contexto.RegistrosTiempo
            .Where(registro => registro.IdUsuario == idUsuarioSeleccionado && registro.FechaRegistro == diaRegistro)
            .SumAsync(registro => (decimal?)registro.Horas) ?? 0m;
        if (horasRegistradasEseDia + horasRedondeadas > 24)
            return Fallo(id, $"Ese día ya tiene {horasRegistradasEseDia:0.##} h registradas entre todas las tareas; el máximo es 24 h.");

        var usuario = await contexto.Usuarios.FindAsync(idUsuarioSeleccionado);
        contexto.RegistrosTiempo.Add(new RegistroTiempo
        {
            IdTarea = id, IdUsuario = idUsuarioSeleccionado, FechaRegistro = diaRegistro, Horas = horasRedondeadas,
            CostoHora = usuario!.CostoHora,   // costo por hora vigente en este momento
            Comentario = comentario?.Trim(), FechaCreacion = DateTime.Now
        });
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(idUsuarioActual, "TAREAS", "REGISTRO DE TIEMPO",
            $"Tarea {codigo}: {horasRedondeadas:0.##} h el {diaRegistro:dd/MM/yyyy} ({usuario.NombreUsuario})");

        TempData["Mensaje"] = usuario.CostoHora == 0
            ? "Tiempo registrado. Aviso: el costo por hora de ese usuario es ₡0; defínalo en Usuarios para que el costo del proyecto sea real."
            : "Tiempo registrado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EliminarRegistro(int id, int idRegistro)
    {
        var registro = await contexto.RegistrosTiempo.FirstOrDefaultAsync(registro =>
            registro.IdRegistro == idRegistro && registro.IdTarea == id);
        if (registro is null) return NotFound();
        // El acceso total elimina cualquiera; los demás, solo el propio y registrado hoy
        var esPropio = registro.IdUsuario == IdActual && registro.FechaCreacion.Date == DateTime.Today;
        if (!Total && !esPropio) return Forbid();

        var codigo = await contexto.Tareas.Where(tarea => tarea.IdTarea == id)
            .Select(tarea => tarea.Codigo).FirstAsync();
        contexto.RegistrosTiempo.Remove(registro);
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "TAREAS", "ELIMINACIÓN",
            $"Tarea {codigo}: registro de {registro.Horas:0.##} h del {registro.FechaRegistro:dd/MM/yyyy}");
        TempData["Mensaje"] = "Registro de tiempo eliminado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- Crear y editar (acceso total) ----------
    /// <summary>Si se llega desde un proyecto (idProyecto), la tarea queda ligada a ese proyecto de forma automática.</summary>
    [RequiereModulo("TAREAS", 2), HttpGet]
    public async Task<IActionResult> Crear(int? idProyecto)
    {
        var idEstadoPendiente = await contexto.Estados.Where(estado =>
                estado.TipoEstado == "TAREA" && estado.Nombre == "Pendiente")
            .Select(estado => estado.IdEstado).FirstOrDefaultAsync();
        var modeloTarea = new TareaViewModel { IdEstado = idEstadoPendiente };

        if (idProyecto is int idProyectoSeleccionado)
        {
            var estadoProyecto = await contexto.Proyectos.Where(proyecto => proyecto.IdProyecto == idProyectoSeleccionado)
                .Select(proyecto => proyecto.EstadoActual.Nombre).FirstOrDefaultAsync();
            if (estadoProyecto is null) TempData["Error"] = "El proyecto indicado no existe.";
            else if (estadoProyecto is "Finalizado" or "Cancelado")
                TempData["Error"] = "El proyecto está finalizado o cancelado y no admite tareas nuevas.";
            else modeloTarea.IdProyecto = idProyectoSeleccionado;
        }
        await PrepararFormulario(modeloTarea);
        return View("Formulario", modeloTarea);
    }

    [RequiereModulo("TAREAS", 2), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(TareaViewModel modeloTarea)
    {
        await Validar(modeloTarea, null, new List<int>());
        if (!ModelState.IsValid) { await PrepararFormulario(modeloTarea); return View("Formulario", modeloTarea); }

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        var tarea = new Tarea
        {
            Nombre = modeloTarea.Nombre.Trim(), Descripcion = modeloTarea.Descripcion?.Trim(),
            IdProyecto = modeloTarea.IdProyecto, IdCategoria = modeloTarea.IdCategoria,
            IdEstado = modeloTarea.IdEstado, FechaInicio = ADate(modeloTarea.FechaInicio),
            FechaFin = ADate(modeloTarea.FechaFin), TiempoEstimado = modeloTarea.TiempoEstimado,
            Comentarios = modeloTarea.Comentarios?.Trim(), FechaCreacion = DateTime.Now
        };
        contexto.Tareas.Add(tarea);
        await contexto.SaveChangesAsync();
        foreach (var idUsuarioAsignado in modeloTarea.IdsUsuarios.Distinct())
            contexto.TareaUsuarios.Add(new TareaUsuario
                { IdTarea = tarea.IdTarea, IdUsuario = idUsuarioAsignado, FechaAsignacion = DateTime.Now });
        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();

        await bitacora.MovimientoAsync(IdActual, "TAREAS", "INSERCIÓN",
            $"Tarea creada: {tarea.Codigo} {tarea.Nombre}");
        TempData["Mensaje"] = $"Tarea creada ({tarea.Codigo}).";
        return RedirectToAction(nameof(Detalle), new { id = tarea.IdTarea });
    }

    [RequiereModulo("TAREAS", 2), HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var tarea = await contexto.Tareas.AsNoTracking().FirstOrDefaultAsync(registro => registro.IdTarea == id);
        if (tarea is null) return NotFound();
        var modeloTarea = new TareaViewModel
        {
            IdTarea = tarea.IdTarea, Codigo = tarea.Codigo, Nombre = tarea.Nombre,
            Descripcion = tarea.Descripcion, IdProyecto = tarea.IdProyecto,
            IdCategoria = tarea.IdCategoria, IdEstado = tarea.IdEstado,
            FechaInicio = tarea.FechaInicio?.ToDateTime(TimeOnly.MinValue),
            FechaFin = tarea.FechaFin?.ToDateTime(TimeOnly.MinValue),
            TiempoEstimado = tarea.TiempoEstimado, Comentarios = tarea.Comentarios,
            IdsUsuarios = await contexto.TareaUsuarios.Where(asignacion => asignacion.IdTarea == id)
                .Select(asignacion => asignacion.IdUsuario).ToListAsync()
        };
        await PrepararFormulario(modeloTarea);
        return View("Formulario", modeloTarea);
    }

    [RequiereModulo("TAREAS", 2), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, TareaViewModel modeloTarea)
    {
        var tarea = await contexto.Tareas.FindAsync(id);
        if (tarea is null) return NotFound();
        modeloTarea.IdTarea = id;
        modeloTarea.Codigo = tarea.Codigo;

        var asignacionesActuales = await contexto.TareaUsuarios.Where(asignacion => asignacion.IdTarea == id).ToListAsync();
        var idsUsuariosActuales = asignacionesActuales.Select(asignacion => asignacion.IdUsuario).ToList();
        await Validar(modeloTarea, tarea.IdProyecto, idsUsuariosActuales);

        // No se puede quitar a quien ya registró tiempo (el costo del proyecto depende de esos registros)
        var idsUsuariosSeleccionados = modeloTarea.IdsUsuarios.Distinct().ToList();
        var idsUsuariosPorQuitar = idsUsuariosActuales.Except(idsUsuariosSeleccionados).ToList();
        if (idsUsuariosPorQuitar.Count > 0)
        {
            var idsUsuariosConTiempo = await contexto.RegistrosTiempo
                .Where(registro => registro.IdTarea == id && idsUsuariosPorQuitar.Contains(registro.IdUsuario))
                .Select(registro => registro.IdUsuario).Distinct().ToListAsync();
            if (idsUsuariosConTiempo.Count > 0)
            {
                var nombresUsuarios = await contexto.Usuarios.Where(usuario => idsUsuariosConTiempo.Contains(usuario.IdUsuario))
                    .Select(usuario => usuario.Nombre + " " + usuario.Apellido1).ToListAsync();
                ModelState.AddModelError(nameof(modeloTarea.IdsUsuarios),
                    $"No puede quitar a {string.Join(", ", nombresUsuarios)}: ya registró tiempo en esta tarea.");
            }
        }
        if (!ModelState.IsValid) { await PrepararFormulario(modeloTarea); return View("Formulario", modeloTarea); }

        await using var transaccion = await contexto.Database.BeginTransactionAsync();
        tarea.Nombre = modeloTarea.Nombre.Trim(); tarea.Descripcion = modeloTarea.Descripcion?.Trim();
        tarea.IdProyecto = modeloTarea.IdProyecto; tarea.IdCategoria = modeloTarea.IdCategoria;
        tarea.IdEstado = modeloTarea.IdEstado; tarea.FechaInicio = ADate(modeloTarea.FechaInicio);
        tarea.FechaFin = ADate(modeloTarea.FechaFin); tarea.TiempoEstimado = modeloTarea.TiempoEstimado;
        tarea.Comentarios = modeloTarea.Comentarios?.Trim();
        contexto.TareaUsuarios.RemoveRange(asignacionesActuales
            .Where(asignacion => idsUsuariosPorQuitar.Contains(asignacion.IdUsuario)));
        foreach (var idUsuarioAsignado in idsUsuariosSeleccionados.Except(idsUsuariosActuales))
            contexto.TareaUsuarios.Add(new TareaUsuario
                { IdTarea = id, IdUsuario = idUsuarioAsignado, FechaAsignacion = DateTime.Now });
        await contexto.SaveChangesAsync();
        await transaccion.CommitAsync();

        await bitacora.MovimientoAsync(IdActual, "TAREAS", "MODIFICACIÓN",
            $"Tarea modificada: {tarea.Codigo} {tarea.Nombre}");
        TempData["Mensaje"] = "Tarea actualizada.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- apoyo ----------
    private async Task Validar(TareaViewModel modeloTarea, int? proyectoActual, List<int> idsYaAsignados)
    {
        if (modeloTarea.FechaInicio is DateTime fechaInicio &&
            modeloTarea.FechaFin is DateTime fechaFin && fechaFin.Date < fechaInicio.Date)
            ModelState.AddModelError(nameof(modeloTarea.FechaFin),
                "La fecha de finalización no puede ser anterior a la de inicio.");

        if (modeloTarea.IdProyecto > 0)
        {
            var estado = await contexto.Proyectos.Where(proyecto => proyecto.IdProyecto == modeloTarea.IdProyecto)
                .Select(proyecto => proyecto.EstadoActual.Nombre).FirstOrDefaultAsync();
            if (estado is null)
                ModelState.AddModelError(nameof(modeloTarea.IdProyecto), "Seleccione un proyecto válido.");
            else if (modeloTarea.IdProyecto != proyectoActual && estado is "Finalizado" or "Cancelado")
                ModelState.AddModelError(nameof(modeloTarea.IdProyecto),
                    "No se pueden agregar tareas a un proyecto finalizado o cancelado.");
        }
        if (modeloTarea.IdCategoria > 0 &&
            !await contexto.Categorias.AnyAsync(categoria => categoria.IdCategoria == modeloTarea.IdCategoria))
            ModelState.AddModelError(nameof(modeloTarea.IdCategoria), "Seleccione una categoría válida.");
        if (modeloTarea.IdEstado > 0 &&
            !await contexto.Estados.AnyAsync(estado => estado.IdEstado == modeloTarea.IdEstado && estado.TipoEstado == "TAREA"))
            ModelState.AddModelError(nameof(modeloTarea.IdEstado), "Seleccione un estado válido.");

        var idsUsuarios = modeloTarea.IdsUsuarios.Distinct().ToList();
        if (idsUsuarios.Count == 0)
        {
            ModelState.AddModelError(nameof(modeloTarea.IdsUsuarios), "Asigne la tarea al menos a un usuario.");
            return;
        }
        if (await contexto.Usuarios.CountAsync(usuario => idsUsuarios.Contains(usuario.IdUsuario)) != idsUsuarios.Count)
            ModelState.AddModelError(nameof(modeloTarea.IdsUsuarios), "Hay usuarios que no existen.");
        var nuevosIdsUsuarios = idsUsuarios.Except(idsYaAsignados).ToList();
        if (nuevosIdsUsuarios.Count > 0 &&
            await contexto.Usuarios.CountAsync(usuario =>
                nuevosIdsUsuarios.Contains(usuario.IdUsuario) && usuario.Estado) != nuevosIdsUsuarios.Count)
            ModelState.AddModelError(nameof(modeloTarea.IdsUsuarios), "Solo se puede asignar a usuarios activos.");
    }

    private async Task PrepararFormulario(TareaViewModel modeloTarea)
    {
        var idProyectoSeleccionado = modeloTarea.IdProyecto;
        var idCategoriaSeleccionada = modeloTarea.IdCategoria;
        var idsUsuariosAsignados = modeloTarea.IdsUsuarios.ToList();

        ViewBag.Proyectos = new SelectList(
            await contexto.Proyectos.AsNoTracking()
                .Where(proyecto => (proyecto.EstadoActual.Nombre != "Finalizado" &&
                                    proyecto.EstadoActual.Nombre != "Cancelado") ||
                                   proyecto.IdProyecto == idProyectoSeleccionado)
                .OrderBy(proyecto => proyecto.Codigo)
                .Select(proyecto => new { proyecto.IdProyecto, Texto = proyecto.Codigo + " · " + proyecto.Nombre }).ToListAsync(),
            "IdProyecto", "Texto", modeloTarea.IdProyecto);
        ViewBag.Categorias = new SelectList(
            await contexto.Categorias.AsNoTracking()
                .Where(categoria => categoria.Estado || categoria.IdCategoria == idCategoriaSeleccionada)
                .OrderBy(categoria => categoria.Nombre).ToListAsync(),
            "IdCategoria", "Nombre", modeloTarea.IdCategoria);
        ViewBag.Estados = new SelectList(
            await contexto.Estados.AsNoTracking().Where(estado => estado.TipoEstado == "TAREA")
                .OrderBy(estado => estado.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", modeloTarea.IdEstado);
        ViewBag.Usuarios = await contexto.Usuarios.AsNoTracking()
            .Where(usuario => usuario.Estado || idsUsuariosAsignados.Contains(usuario.IdUsuario))
            .OrderBy(usuario => usuario.Nombre)
            .Select(usuario => new OpcionUsuario(usuario.IdUsuario,
                usuario.Nombre + " " + usuario.Apellido1 + (usuario.Estado ? "" : " (inactivo)")))
            .ToListAsync();
    }

    private async Task CargarFiltros(int? idProyecto, int? idEstado, int? idUsuario)
    {
        ViewBag.Proyectos = new SelectList(
            await contexto.Proyectos.AsNoTracking().OrderBy(proyecto => proyecto.Codigo)
                .Select(proyecto => new { proyecto.IdProyecto, Texto = proyecto.Codigo + " · " + proyecto.Nombre }).ToListAsync(),
            "IdProyecto", "Texto", idProyecto);
        ViewBag.Estados = new SelectList(
            await contexto.Estados.AsNoTracking().Where(estado => estado.TipoEstado == "TAREA")
                .OrderBy(estado => estado.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", idEstado);
        if (Total)
            ViewBag.Usuarios = new SelectList(
                await contexto.Usuarios.AsNoTracking().OrderBy(usuario => usuario.Nombre)
                    .Select(usuario => new { usuario.IdUsuario, Texto = usuario.Nombre + " " + usuario.Apellido1 }).ToListAsync(),
                "IdUsuario", "Texto", idUsuario);
    }
}
