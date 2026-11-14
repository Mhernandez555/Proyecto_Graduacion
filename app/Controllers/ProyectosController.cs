using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

/// <summary>
/// Gestión de proyectos (RF-003). Acceso total (Administrador, Soporte TI): crea, edita y ve precio, costo y rentabilidad.
/// Acceso parcial (Empleado): solo consulta los proyectos en los que participa, sin montos.
/// </summary>
[RequiereModulo("PROYECTOS", 1)]
public class ProyectosController(ControlexDbContext contexto, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Total => int.TryParse(User.FindFirst("perm:PROYECTOS")?.Value, out var nivel) && nivel >= 2;
    private static DateOnly? ADate(DateTime? fecha) => fecha is null ? (DateOnly?)null : DateOnly.FromDateTime(fecha.Value);

    /// <summary>Con acceso parcial: proyectos que dirige o en los que tiene alguna tarea asignada.</summary>
    private IQueryable<Proyecto> Visibles(IQueryable<Proyecto> consulta)
    {
        if (Total) return consulta;
        var idUsuarioActual = IdActual;
        return consulta.Where(proyecto => proyecto.IdResponsable == idUsuarioActual ||
            contexto.Tareas.Any(tarea => tarea.IdProyecto == proyecto.IdProyecto &&
                contexto.TareaUsuarios.Any(asignacion => asignacion.IdTarea == tarea.IdTarea &&
                    asignacion.IdUsuario == idUsuarioActual)));
    }

    private Task<bool> Participa(int idProyecto) =>
        Visibles(contexto.Proyectos).AnyAsync(proyecto => proyecto.IdProyecto == idProyecto);

    private static RouteValueDictionary RutaIndex(int? idCliente, int? idEstado, string? texto)
    {
        var valoresRuta = new RouteValueDictionary();
        if (idCliente is int clienteSeleccionado) valoresRuta["idCliente"] = clienteSeleccionado;
        if (idEstado is int estadoSeleccionado) valoresRuta["idEstado"] = estadoSeleccionado;
        if (!string.IsNullOrWhiteSpace(texto)) valoresRuta["texto"] = texto;
        return valoresRuta;
    }

    // ---------- Ver proyectos ----------
    public async Task<IActionResult> Index(int? idCliente, int? idEstado, string? texto, int pagina = 1)
    {
        var consulta = Visibles(contexto.Proyectos.AsNoTracking());
        if (idCliente is int clienteSeleccionado)
            consulta = consulta.Where(proyecto => proyecto.IdCliente == clienteSeleccionado);
        if (idEstado is int estadoSeleccionado)
            consulta = consulta.Where(proyecto => proyecto.IdEstado == estadoSeleccionado);
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var textoBusqueda = texto.Trim();
            consulta = consulta.Where(proyecto =>
                proyecto.Nombre.Contains(textoBusqueda) || proyecto.Codigo.Contains(textoBusqueda));
        }

        var paginacion = Paginacion.Crear("Proyectos", nameof(Index), pagina, await consulta.CountAsync(),
            RutaIndex(idCliente, idEstado, texto));
        var proyectos = await consulta.OrderByDescending(proyecto => proyecto.IdProyecto)
            .Skip((paginacion.Pagina - 1) * paginacion.Tam).Take(paginacion.Tam)
            .Select(proyecto => new
        {
            proyecto.IdProyecto, proyecto.Codigo, proyecto.Nombre,
            Cliente = proyecto.Cliente.NombreCliente, Categoria = proyecto.Categoria.Nombre, Estado = proyecto.EstadoActual.Nombre,
            Responsable = proyecto.Responsable.Nombre + " " + proyecto.Responsable.Apellido1,
            proyecto.FechaInicio, proyecto.FechaFin, proyecto.PrecioVenta
        }).ToListAsync();

        var idsProyectos = proyectos.Select(proyecto => proyecto.IdProyecto).ToList();
        var tareas = (await contexto.Tareas.AsNoTracking().Where(tarea => idsProyectos.Contains(tarea.IdProyecto))
            .Select(tarea => new { tarea.IdProyecto, Completada = tarea.EstadoActual.Nombre == "Completada" }).ToListAsync())
            .ToLookup(tarea => tarea.IdProyecto);
        var finanzas = Total
            ? await contexto.RentabilidadProyectos.AsNoTracking().Where(resumen => idsProyectos.Contains(resumen.IdProyecto))
                .ToDictionaryAsync(resumen => resumen.IdProyecto)
            : new Dictionary<int, VistaRentabilidad>();

        var filas = proyectos.Select(proyecto =>
        {
            finanzas.TryGetValue(proyecto.IdProyecto, out var finanzasProyecto);
            return new ProyectoListado(proyecto.IdProyecto, proyecto.Codigo, proyecto.Nombre, proyecto.Cliente,
                proyecto.Categoria, proyecto.Estado, proyecto.Responsable, proyecto.FechaInicio, proyecto.FechaFin,
                tareas[proyecto.IdProyecto].Count(), tareas[proyecto.IdProyecto].Count(tarea => tarea.Completada),
                Total ? proyecto.PrecioVenta : null, Total ? finanzasProyecto?.CostoAcumulado ?? 0m : null,
                Total ? finanzasProyecto?.Rentabilidad ?? proyecto.PrecioVenta : null);
        }).ToList();

        ViewBag.Clientes = new SelectList(
            await contexto.Clientes.AsNoTracking().OrderBy(cliente => cliente.NombreCliente).ToListAsync(),
            "IdCliente", "NombreCliente", idCliente);
        ViewBag.Estados = new SelectList(
            await contexto.Estados.AsNoTracking().Where(estado => estado.TipoEstado == "PROYECTO")
                .OrderBy(estado => estado.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", idEstado);
        ViewBag.Texto = texto;
        return View(new ProyectosIndexViewModel { Filas = filas, Total = Total, Paginacion = paginacion });
    }

    // ---------- Consultar ----------
    public async Task<IActionResult> Detalle(int id, int pagina = 1)
    {
        if (!await Participa(id))
            return await contexto.Proyectos.AnyAsync(proyecto => proyecto.IdProyecto == id) ? Forbid() : NotFound();

        var proyecto = await contexto.Proyectos.AsNoTracking().Where(registro => registro.IdProyecto == id).Select(registro => new
        {
            registro.IdProyecto, registro.Codigo, registro.Nombre, registro.Descripcion,
            Cliente = registro.Cliente.NombreCliente, Categoria = registro.Categoria.Nombre, registro.IdEstado,
            Estado = registro.EstadoActual.Nombre,
            Responsable = registro.Responsable.Nombre + " " + registro.Responsable.Apellido1,
            registro.FechaInicio, registro.FechaFin, registro.PrecioVenta
        }).FirstAsync();

        var idUsuarioActual = IdActual;
        var consultaTareas = contexto.Tareas.AsNoTracking().Where(tarea => tarea.IdProyecto == id);
        if (!Total)
            consultaTareas = consultaTareas.Where(tarea =>
                contexto.TareaUsuarios.Any(asignacion => asignacion.IdTarea == tarea.IdTarea &&
                    asignacion.IdUsuario == idUsuarioActual));
        var totalTareas = await consultaTareas.CountAsync();
        var tareasCompletadas = await consultaTareas.CountAsync(tarea => tarea.EstadoActual.Nombre == "Completada");
        var paginacion = Paginacion.Crear("Proyectos", nameof(Detalle), pagina, totalTareas,
            new RouteValueDictionary { ["id"] = id });
        var tareas = await consultaTareas.OrderBy(tarea => tarea.IdTarea)
            .Skip((paginacion.Pagina - 1) * paginacion.Tam).Take(paginacion.Tam).Select(tarea => new
        {
            tarea.IdTarea, tarea.Codigo, tarea.Nombre, Categoria = tarea.Categoria.Nombre, Estado = tarea.EstadoActual.Nombre,
            tarea.FechaInicio, tarea.FechaFin, tarea.TiempoEstimado
        }).ToListAsync();
        var idsTareas = tareas.Select(tarea => tarea.IdTarea).ToList();
        var asignados = (await contexto.TareaUsuarios.AsNoTracking().Where(asignacion => idsTareas.Contains(asignacion.IdTarea))
            .Select(asignacion => new
            {
                asignacion.IdTarea,
                Nombre = asignacion.Usuario.Nombre + " " + asignacion.Usuario.Apellido1
            }).ToListAsync())
            .ToLookup(asignacion => asignacion.IdTarea, asignacion => asignacion.Nombre);
        var horasPorTarea = await contexto.RegistrosTiempo.AsNoTracking().Where(registro => idsTareas.Contains(registro.IdTarea))
            .GroupBy(registro => registro.IdTarea)
            .Select(grupo => new { IdTarea = grupo.Key, Horas = grupo.Sum(registro => registro.Horas) })
            .ToDictionaryAsync(resumen => resumen.IdTarea, resumen => resumen.Horas);

        var modeloVista = new ProyectoDetalleViewModel
        {
            IdProyecto = proyecto.IdProyecto, Codigo = proyecto.Codigo, Nombre = proyecto.Nombre,
            Descripcion = proyecto.Descripcion, Cliente = proyecto.Cliente,
            Categoria = proyecto.Categoria, Estado = proyecto.Estado, Responsable = proyecto.Responsable,
            FechaInicio = proyecto.FechaInicio, FechaFin = proyecto.FechaFin,
            VerFinanzas = Total, PuedeEditar = Total,
            TotalTareas = totalTareas, TareasCompletadas = tareasCompletadas, Paginacion = paginacion,
            Tareas = tareas.Select(tarea => new TareaListado(tarea.IdTarea, tarea.Codigo, tarea.Nombre,
                proyecto.Nombre, tarea.Categoria, string.Join(", ", asignados[tarea.IdTarea]), tarea.Estado,
                tarea.FechaInicio, tarea.FechaFin, tarea.TiempoEstimado,
                horasPorTarea.GetValueOrDefault(tarea.IdTarea, 0m))).ToList()
        };
        if (Total)
        {
            modeloVista.Precio = proyecto.PrecioVenta;
            modeloVista.Costo = (await contexto.RentabilidadProyectos.AsNoTracking()
                .FirstOrDefaultAsync(resumen => resumen.IdProyecto == id))?.CostoAcumulado ?? 0m;
            ViewBag.Estados = new SelectList(
                await contexto.Estados.AsNoTracking().Where(estado => estado.TipoEstado == "PROYECTO")
                    .OrderBy(estado => estado.IdEstado).ToListAsync(),
                "IdEstado", "Nombre", proyecto.IdEstado);
        }
        return View(modeloVista);
    }

    [RequiereModulo("PROYECTOS", 2), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, int idEstado)
    {
        var proyecto = await contexto.Proyectos.FindAsync(id);
        if (proyecto is null) return NotFound();
        var nuevoEstado = await contexto.Estados.FirstOrDefaultAsync(estado =>
            estado.IdEstado == idEstado && estado.TipoEstado == "PROYECTO");
        if (nuevoEstado is null) { TempData["Error"] = "Seleccione un estado válido."; return RedirectToAction(nameof(Detalle), new { id }); }

        if (nuevoEstado.IdEstado != proyecto.IdEstado && nuevoEstado.Nombre == "Finalizado")
        {
            var pendientes = await TareasSinCompletar(id);
            if (pendientes > 0)
            {
                TempData["Error"] = $"No se puede finalizar el proyecto: hay {pendientes} tarea(s) sin completar.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
        }

        var estadoAnterior = await contexto.Estados.Where(estado => estado.IdEstado == proyecto.IdEstado)
            .Select(estado => estado.Nombre).FirstAsync();
        proyecto.IdEstado = nuevoEstado.IdEstado;
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "PROYECTOS", "CAMBIO DE ESTADO",
            $"Proyecto {proyecto.Codigo}: {estadoAnterior} → {nuevoEstado.Nombre}");
        TempData["Mensaje"] = "Estado del proyecto actualizado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- Crear y editar (acceso total) ----------
    [RequiereModulo("PROYECTOS", 2), HttpGet]
    public async Task<IActionResult> Crear()
    {
        var idEstadoPlanificado = await contexto.Estados.Where(estado =>
                estado.TipoEstado == "PROYECTO" && estado.Nombre == "Planificado")
            .Select(estado => estado.IdEstado).FirstOrDefaultAsync();
        var modeloProyecto = new ProyectoViewModel
            { IdEstado = idEstadoPlanificado, FechaInicio = DateTime.Today, IdResponsable = IdActual };
        await PrepararFormulario(modeloProyecto);
        return View("Formulario", modeloProyecto);
    }

    [RequiereModulo("PROYECTOS", 2), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProyectoViewModel modeloProyecto)
    {
        await Validar(modeloProyecto, null);
        if (!ModelState.IsValid) { await PrepararFormulario(modeloProyecto); return View("Formulario", modeloProyecto); }

        var proyecto = new Proyecto
        {
            Nombre = modeloProyecto.Nombre.Trim(), Descripcion = modeloProyecto.Descripcion?.Trim(),
            IdCliente = modeloProyecto.IdCliente, IdCategoria = modeloProyecto.IdCategoria,
            IdEstado = modeloProyecto.IdEstado, IdResponsable = modeloProyecto.IdResponsable,
            FechaInicio = DateOnly.FromDateTime(modeloProyecto.FechaInicio!.Value),
            FechaFin = ADate(modeloProyecto.FechaFin), PrecioVenta = Math.Round(modeloProyecto.PrecioVenta, 2),
            FechaCreacion = DateTime.Now
        };
        contexto.Proyectos.Add(proyecto);
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "PROYECTOS", "INSERCIÓN",
            $"Proyecto creado: {proyecto.Codigo} {proyecto.Nombre}");

        TempData["Mensaje"] = $"Proyecto creado ({proyecto.Codigo}).";
        return RedirectToAction(nameof(Detalle), new { id = proyecto.IdProyecto });
    }

    [RequiereModulo("PROYECTOS", 2), HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var proyecto = await contexto.Proyectos.AsNoTracking().FirstOrDefaultAsync(registro => registro.IdProyecto == id);
        if (proyecto is null) return NotFound();
        var modeloProyecto = new ProyectoViewModel
        {
            IdProyecto = proyecto.IdProyecto, Codigo = proyecto.Codigo, Nombre = proyecto.Nombre,
            Descripcion = proyecto.Descripcion, IdCliente = proyecto.IdCliente,
            IdCategoria = proyecto.IdCategoria, IdEstado = proyecto.IdEstado, IdResponsable = proyecto.IdResponsable,
            FechaInicio = proyecto.FechaInicio.ToDateTime(TimeOnly.MinValue),
            FechaFin = proyecto.FechaFin?.ToDateTime(TimeOnly.MinValue), PrecioVenta = proyecto.PrecioVenta
        };
        await PrepararFormulario(modeloProyecto);
        return View("Formulario", modeloProyecto);
    }

    [RequiereModulo("PROYECTOS", 2), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ProyectoViewModel modeloProyecto)
    {
        var proyecto = await contexto.Proyectos.FindAsync(id);
        if (proyecto is null) return NotFound();
        modeloProyecto.IdProyecto = id;
        modeloProyecto.Codigo = proyecto.Codigo;

        await Validar(modeloProyecto, proyecto);
        if (!ModelState.IsValid) { await PrepararFormulario(modeloProyecto); return View("Formulario", modeloProyecto); }

        proyecto.Nombre = modeloProyecto.Nombre.Trim(); proyecto.Descripcion = modeloProyecto.Descripcion?.Trim();
        proyecto.IdCliente = modeloProyecto.IdCliente; proyecto.IdCategoria = modeloProyecto.IdCategoria;
        proyecto.IdEstado = modeloProyecto.IdEstado; proyecto.IdResponsable = modeloProyecto.IdResponsable;
        proyecto.FechaInicio = DateOnly.FromDateTime(modeloProyecto.FechaInicio!.Value);
        proyecto.FechaFin = ADate(modeloProyecto.FechaFin);
        proyecto.PrecioVenta = Math.Round(modeloProyecto.PrecioVenta, 2);
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "PROYECTOS", "MODIFICACIÓN",
            $"Proyecto modificado: {proyecto.Codigo} {proyecto.Nombre}");

        TempData["Mensaje"] = "Proyecto actualizado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- apoyo ----------
    private Task<int> TareasSinCompletar(int idProyecto) =>
        contexto.Tareas.CountAsync(tarea =>
            tarea.IdProyecto == idProyecto && tarea.EstadoActual.Nombre != "Completada");

    private async Task Validar(ProyectoViewModel modeloProyecto, Proyecto? proyectoActual)
    {
        if (modeloProyecto.FechaInicio is DateTime fechaInicio &&
            modeloProyecto.FechaFin is DateTime fechaFin && fechaFin.Date < fechaInicio.Date)
            ModelState.AddModelError(nameof(modeloProyecto.FechaFin), "La fecha de finalización no puede ser anterior a la de inicio.");

        if (modeloProyecto.IdCliente > 0)
        {
            var cliente = await contexto.Clientes.Where(registro => registro.IdCliente == modeloProyecto.IdCliente)
                .Select(registro => new { registro.Estado }).FirstOrDefaultAsync();
            if (cliente is null) ModelState.AddModelError(nameof(modeloProyecto.IdCliente), "Seleccione un cliente válido.");
            else if (!cliente.Estado && proyectoActual?.IdCliente != modeloProyecto.IdCliente)
                ModelState.AddModelError(nameof(modeloProyecto.IdCliente), "El cliente está inactivo.");
        }
        if (modeloProyecto.IdCategoria > 0)
        {
            var categoria = await contexto.Categorias.Where(registro => registro.IdCategoria == modeloProyecto.IdCategoria)
                .Select(registro => new { registro.Estado }).FirstOrDefaultAsync();
            if (categoria is null) ModelState.AddModelError(nameof(modeloProyecto.IdCategoria), "Seleccione una categoría válida.");
            else if (!categoria.Estado && proyectoActual?.IdCategoria != modeloProyecto.IdCategoria)
                ModelState.AddModelError(nameof(modeloProyecto.IdCategoria), "La categoría está inactiva.");
        }
        if (modeloProyecto.IdResponsable > 0)
        {
            var responsable = await contexto.Usuarios.Where(usuario => usuario.IdUsuario == modeloProyecto.IdResponsable)
                .Select(usuario => new { usuario.Estado }).FirstOrDefaultAsync();
            if (responsable is null) ModelState.AddModelError(nameof(modeloProyecto.IdResponsable), "Seleccione un responsable válido.");
            else if (!responsable.Estado && proyectoActual?.IdResponsable != modeloProyecto.IdResponsable)
                ModelState.AddModelError(nameof(modeloProyecto.IdResponsable), "El responsable está inactivo.");
        }
        if (modeloProyecto.IdEstado > 0)
        {
            var estado = await contexto.Estados.Where(registro =>
                    registro.IdEstado == modeloProyecto.IdEstado && registro.TipoEstado == "PROYECTO")
                .Select(registro => registro.Nombre).FirstOrDefaultAsync();
            if (estado is null) ModelState.AddModelError(nameof(modeloProyecto.IdEstado), "Seleccione un estado válido.");
            else if (proyectoActual is not null && proyectoActual.IdEstado != modeloProyecto.IdEstado && estado == "Finalizado")
            {
                var pendientes = await TareasSinCompletar(proyectoActual.IdProyecto);
                if (pendientes > 0)
                    ModelState.AddModelError(nameof(modeloProyecto.IdEstado),
                        $"No se puede finalizar el proyecto: hay {pendientes} tarea(s) sin completar.");
            }
        }
    }

    private async Task PrepararFormulario(ProyectoViewModel modeloProyecto)
    {
        var idClienteSeleccionado = modeloProyecto.IdCliente;
        var idCategoriaSeleccionada = modeloProyecto.IdCategoria;
        var idResponsableSeleccionado = modeloProyecto.IdResponsable;

        ViewBag.Clientes = new SelectList(
            await contexto.Clientes.AsNoTracking()
                .Where(cliente => cliente.Estado || cliente.IdCliente == idClienteSeleccionado)
                .OrderBy(cliente => cliente.NombreCliente).ToListAsync(),
            "IdCliente", "NombreCliente", modeloProyecto.IdCliente);
        ViewBag.Categorias = new SelectList(
            await contexto.Categorias.AsNoTracking()
                .Where(categoria => categoria.Estado || categoria.IdCategoria == idCategoriaSeleccionada)
                .OrderBy(categoria => categoria.Nombre).ToListAsync(),
            "IdCategoria", "Nombre", modeloProyecto.IdCategoria);
        ViewBag.Estados = new SelectList(
            await contexto.Estados.AsNoTracking().Where(estado => estado.TipoEstado == "PROYECTO")
                .OrderBy(estado => estado.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", modeloProyecto.IdEstado);
        ViewBag.Responsables = new SelectList(
            await contexto.Usuarios.AsNoTracking()
                .Where(usuario => usuario.Estado || usuario.IdUsuario == idResponsableSeleccionado)
                .OrderBy(usuario => usuario.Nombre)
                .Select(usuario => new { usuario.IdUsuario, Texto = usuario.Nombre + " " + usuario.Apellido1 }).ToListAsync(),
            "IdUsuario", "Texto", modeloProyecto.IdResponsable);
    }
}
