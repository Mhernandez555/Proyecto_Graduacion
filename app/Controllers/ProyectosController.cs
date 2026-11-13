using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

/// <summary>
/// Gestión de proyectos (RF-003). Acceso total (Administrador, Soporte TI): crea, edita y ve precio, costo y rentabilidad.
/// Acceso parcial (Empleado): solo consulta los proyectos en los que participa, sin montos.
/// </summary>
[RequiereModulo("PROYECTOS", 1)]
public class ProyectosController(ControlexDbContext db, BitacoraService bitacora) : Controller
{
    private const int Limite = 200;
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private bool Total => int.TryParse(User.FindFirst("perm:PROYECTOS")?.Value, out var n) && n >= 2;
    private static DateOnly? ADate(DateTime? d) => d is null ? (DateOnly?)null : DateOnly.FromDateTime(d.Value);

    /// <summary>Con acceso parcial: proyectos que dirige o en los que tiene alguna tarea asignada.</summary>
    private IQueryable<Proyecto> Visibles(IQueryable<Proyecto> q)
    {
        if (Total) return q;
        var yo = IdActual;
        return q.Where(p => p.IdResponsable == yo ||
            db.Tareas.Any(t => t.IdProyecto == p.IdProyecto &&
                               db.TareaUsuarios.Any(a => a.IdTarea == t.IdTarea && a.IdUsuario == yo)));
    }

    private Task<bool> Participa(int idProyecto) => Visibles(db.Proyectos).AnyAsync(p => p.IdProyecto == idProyecto);

    // ---------- Ver proyectos ----------
    public async Task<IActionResult> Index(int? idCliente, int? idEstado, string? texto)
    {
        var q = Visibles(db.Proyectos.AsNoTracking());
        if (idCliente is int c) q = q.Where(p => p.IdCliente == c);
        if (idEstado is int e) q = q.Where(p => p.IdEstado == e);
        if (!string.IsNullOrWhiteSpace(texto))
        {
            var tx = texto.Trim();
            q = q.Where(p => p.Nombre.Contains(tx) || p.Codigo.Contains(tx));
        }

        var proyectos = await q.OrderByDescending(p => p.IdProyecto).Take(Limite + 1).Select(p => new
        {
            p.IdProyecto, p.Codigo, p.Nombre,
            Cliente = p.Cliente.NombreCliente, Categoria = p.Categoria.Nombre, Estado = p.EstadoActual.Nombre,
            Responsable = p.Responsable.Nombre + " " + p.Responsable.Apellido1,
            p.FechaInicio, p.FechaFin, p.PrecioVenta
        }).ToListAsync();
        var hayMas = proyectos.Count > Limite;
        if (hayMas) proyectos = proyectos.Take(Limite).ToList();

        var ids = proyectos.Select(p => p.IdProyecto).ToList();
        var tareas = (await db.Tareas.AsNoTracking().Where(t => ids.Contains(t.IdProyecto))
            .Select(t => new { t.IdProyecto, Completada = t.EstadoActual.Nombre == "Completada" }).ToListAsync())
            .ToLookup(t => t.IdProyecto);
        var finanzas = Total
            ? await db.RentabilidadProyectos.AsNoTracking().Where(v => ids.Contains(v.IdProyecto)).ToDictionaryAsync(v => v.IdProyecto)
            : new Dictionary<int, VistaRentabilidad>();

        var filas = proyectos.Select(p =>
        {
            finanzas.TryGetValue(p.IdProyecto, out var f);
            return new ProyectoListado(p.IdProyecto, p.Codigo, p.Nombre, p.Cliente, p.Categoria, p.Estado, p.Responsable,
                p.FechaInicio, p.FechaFin, tareas[p.IdProyecto].Count(), tareas[p.IdProyecto].Count(t => t.Completada),
                Total ? p.PrecioVenta : null, Total ? f?.CostoAcumulado ?? 0m : null, Total ? f?.Rentabilidad ?? p.PrecioVenta : null);
        }).ToList();

        ViewBag.Clientes = new SelectList(
            await db.Clientes.AsNoTracking().OrderBy(x => x.NombreCliente).ToListAsync(), "IdCliente", "NombreCliente", idCliente);
        ViewBag.Estados = new SelectList(
            await db.Estados.AsNoTracking().Where(x => x.TipoEstado == "PROYECTO").OrderBy(x => x.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", idEstado);
        ViewBag.Texto = texto;
        return View(new ProyectosIndexViewModel { Filas = filas, HayMas = hayMas, Total = Total, Limite = Limite });
    }

    // ---------- Consultar ----------
    public async Task<IActionResult> Detalle(int id)
    {
        if (!await Participa(id))
            return await db.Proyectos.AnyAsync(p => p.IdProyecto == id) ? Forbid() : NotFound();

        var p = await db.Proyectos.AsNoTracking().Where(x => x.IdProyecto == id).Select(x => new
        {
            x.IdProyecto, x.Codigo, x.Nombre, x.Descripcion,
            Cliente = x.Cliente.NombreCliente, Categoria = x.Categoria.Nombre, x.IdEstado, Estado = x.EstadoActual.Nombre,
            Responsable = x.Responsable.Nombre + " " + x.Responsable.Apellido1,
            x.FechaInicio, x.FechaFin, x.PrecioVenta
        }).FirstAsync();

        var yo = IdActual;
        var qt = db.Tareas.AsNoTracking().Where(t => t.IdProyecto == id);
        if (!Total) qt = qt.Where(t => db.TareaUsuarios.Any(a => a.IdTarea == t.IdTarea && a.IdUsuario == yo));
        var tareas = await qt.OrderBy(t => t.IdTarea).Select(t => new
        {
            t.IdTarea, t.Codigo, t.Nombre, Categoria = t.Categoria.Nombre, Estado = t.EstadoActual.Nombre,
            t.FechaInicio, t.FechaFin, t.TiempoEstimado
        }).ToListAsync();
        var tids = tareas.Select(t => t.IdTarea).ToList();
        var asignados = (await db.TareaUsuarios.AsNoTracking().Where(a => tids.Contains(a.IdTarea))
            .Select(a => new { a.IdTarea, Nombre = a.Usuario.Nombre + " " + a.Usuario.Apellido1 }).ToListAsync())
            .ToLookup(a => a.IdTarea, a => a.Nombre);
        var horas = await db.RegistrosTiempo.AsNoTracking().Where(r => tids.Contains(r.IdTarea))
            .GroupBy(r => r.IdTarea).Select(g => new { IdTarea = g.Key, Horas = g.Sum(r => r.Horas) })
            .ToDictionaryAsync(x => x.IdTarea, x => x.Horas);

        var vm = new ProyectoDetalleViewModel
        {
            IdProyecto = p.IdProyecto, Codigo = p.Codigo, Nombre = p.Nombre, Descripcion = p.Descripcion, Cliente = p.Cliente,
            Categoria = p.Categoria, Estado = p.Estado, Responsable = p.Responsable, FechaInicio = p.FechaInicio, FechaFin = p.FechaFin,
            VerFinanzas = Total, PuedeEditar = Total,
            Tareas = tareas.Select(t => new TareaListado(t.IdTarea, t.Codigo, t.Nombre, p.Nombre, t.Categoria,
                string.Join(", ", asignados[t.IdTarea]), t.Estado, t.FechaInicio, t.FechaFin,
                t.TiempoEstimado, horas.GetValueOrDefault(t.IdTarea, 0m))).ToList()
        };
        if (Total)
        {
            vm.Precio = p.PrecioVenta;
            vm.Costo = (await db.RentabilidadProyectos.AsNoTracking().FirstOrDefaultAsync(v => v.IdProyecto == id))?.CostoAcumulado ?? 0m;
            ViewBag.Estados = new SelectList(
                await db.Estados.AsNoTracking().Where(e => e.TipoEstado == "PROYECTO").OrderBy(e => e.IdEstado).ToListAsync(),
                "IdEstado", "Nombre", p.IdEstado);
        }
        return View(vm);
    }

    [RequiereModulo("PROYECTOS", 2), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, int idEstado)
    {
        var p = await db.Proyectos.FindAsync(id);
        if (p is null) return NotFound();
        var nuevo = await db.Estados.FirstOrDefaultAsync(e => e.IdEstado == idEstado && e.TipoEstado == "PROYECTO");
        if (nuevo is null) { TempData["Error"] = "Seleccione un estado válido."; return RedirectToAction(nameof(Detalle), new { id }); }

        if (nuevo.IdEstado != p.IdEstado && nuevo.Nombre == "Finalizado")
        {
            var pendientes = await TareasSinCompletar(id);
            if (pendientes > 0)
            {
                TempData["Error"] = $"No se puede finalizar el proyecto: hay {pendientes} tarea(s) sin completar.";
                return RedirectToAction(nameof(Detalle), new { id });
            }
        }

        var anterior = await db.Estados.Where(e => e.IdEstado == p.IdEstado).Select(e => e.Nombre).FirstAsync();
        p.IdEstado = nuevo.IdEstado;
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "PROYECTOS", "CAMBIO DE ESTADO", $"Proyecto {p.Codigo}: {anterior} → {nuevo.Nombre}");
        TempData["Mensaje"] = "Estado del proyecto actualizado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- Crear y editar (acceso total) ----------
    [RequiereModulo("PROYECTOS", 2), HttpGet]
    public async Task<IActionResult> Crear()
    {
        var planificado = await db.Estados.Where(e => e.TipoEstado == "PROYECTO" && e.Nombre == "Planificado")
            .Select(e => e.IdEstado).FirstOrDefaultAsync();
        var m = new ProyectoViewModel { IdEstado = planificado, FechaInicio = DateTime.Today, IdResponsable = IdActual };
        await PrepararFormulario(m);
        return View("Formulario", m);
    }

    [RequiereModulo("PROYECTOS", 2), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProyectoViewModel m)
    {
        await Validar(m, null);
        if (!ModelState.IsValid) { await PrepararFormulario(m); return View("Formulario", m); }

        var p = new Proyecto
        {
            Nombre = m.Nombre.Trim(), Descripcion = m.Descripcion?.Trim(), IdCliente = m.IdCliente, IdCategoria = m.IdCategoria,
            IdEstado = m.IdEstado, IdResponsable = m.IdResponsable, FechaInicio = DateOnly.FromDateTime(m.FechaInicio!.Value),
            FechaFin = ADate(m.FechaFin), PrecioVenta = Math.Round(m.PrecioVenta, 2), FechaCreacion = DateTime.Now
        };
        db.Proyectos.Add(p);
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "PROYECTOS", "INSERCIÓN", $"Proyecto creado: {p.Codigo} {p.Nombre}");

        TempData["Mensaje"] = $"Proyecto creado ({p.Codigo}).";
        return RedirectToAction(nameof(Detalle), new { id = p.IdProyecto });
    }

    [RequiereModulo("PROYECTOS", 2), HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var p = await db.Proyectos.AsNoTracking().FirstOrDefaultAsync(x => x.IdProyecto == id);
        if (p is null) return NotFound();
        var m = new ProyectoViewModel
        {
            IdProyecto = p.IdProyecto, Codigo = p.Codigo, Nombre = p.Nombre, Descripcion = p.Descripcion, IdCliente = p.IdCliente,
            IdCategoria = p.IdCategoria, IdEstado = p.IdEstado, IdResponsable = p.IdResponsable,
            FechaInicio = p.FechaInicio.ToDateTime(TimeOnly.MinValue), FechaFin = p.FechaFin?.ToDateTime(TimeOnly.MinValue),
            PrecioVenta = p.PrecioVenta
        };
        await PrepararFormulario(m);
        return View("Formulario", m);
    }

    [RequiereModulo("PROYECTOS", 2), HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ProyectoViewModel m)
    {
        var p = await db.Proyectos.FindAsync(id);
        if (p is null) return NotFound();
        m.IdProyecto = id;
        m.Codigo = p.Codigo;

        await Validar(m, p);
        if (!ModelState.IsValid) { await PrepararFormulario(m); return View("Formulario", m); }

        p.Nombre = m.Nombre.Trim(); p.Descripcion = m.Descripcion?.Trim(); p.IdCliente = m.IdCliente; p.IdCategoria = m.IdCategoria;
        p.IdEstado = m.IdEstado; p.IdResponsable = m.IdResponsable; p.FechaInicio = DateOnly.FromDateTime(m.FechaInicio!.Value);
        p.FechaFin = ADate(m.FechaFin); p.PrecioVenta = Math.Round(m.PrecioVenta, 2);
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "PROYECTOS", "MODIFICACIÓN", $"Proyecto modificado: {p.Codigo} {p.Nombre}");

        TempData["Mensaje"] = "Proyecto actualizado.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    // ---------- apoyo ----------
    private Task<int> TareasSinCompletar(int idProyecto) =>
        db.Tareas.CountAsync(t => t.IdProyecto == idProyecto && t.EstadoActual.Nombre != "Completada");

    private async Task Validar(ProyectoViewModel m, Proyecto? actual)
    {
        if (m.FechaInicio is DateTime i && m.FechaFin is DateTime f && f.Date < i.Date)
            ModelState.AddModelError(nameof(m.FechaFin), "La fecha de finalización no puede ser anterior a la de inicio.");

        if (m.IdCliente > 0)
        {
            var cliente = await db.Clientes.Where(c => c.IdCliente == m.IdCliente).Select(c => new { c.Estado }).FirstOrDefaultAsync();
            if (cliente is null) ModelState.AddModelError(nameof(m.IdCliente), "Seleccione un cliente válido.");
            else if (!cliente.Estado && actual?.IdCliente != m.IdCliente) ModelState.AddModelError(nameof(m.IdCliente), "El cliente está inactivo.");
        }
        if (m.IdCategoria > 0)
        {
            var categoria = await db.Categorias.Where(c => c.IdCategoria == m.IdCategoria).Select(c => new { c.Estado }).FirstOrDefaultAsync();
            if (categoria is null) ModelState.AddModelError(nameof(m.IdCategoria), "Seleccione una categoría válida.");
            else if (!categoria.Estado && actual?.IdCategoria != m.IdCategoria) ModelState.AddModelError(nameof(m.IdCategoria), "La categoría está inactiva.");
        }
        if (m.IdResponsable > 0)
        {
            var responsable = await db.Usuarios.Where(u => u.IdUsuario == m.IdResponsable).Select(u => new { u.Estado }).FirstOrDefaultAsync();
            if (responsable is null) ModelState.AddModelError(nameof(m.IdResponsable), "Seleccione un responsable válido.");
            else if (!responsable.Estado && actual?.IdResponsable != m.IdResponsable) ModelState.AddModelError(nameof(m.IdResponsable), "El responsable está inactivo.");
        }
        if (m.IdEstado > 0)
        {
            var estado = await db.Estados.Where(e => e.IdEstado == m.IdEstado && e.TipoEstado == "PROYECTO")
                .Select(e => e.Nombre).FirstOrDefaultAsync();
            if (estado is null) ModelState.AddModelError(nameof(m.IdEstado), "Seleccione un estado válido.");
            else if (actual is not null && actual.IdEstado != m.IdEstado && estado == "Finalizado")
            {
                var pendientes = await TareasSinCompletar(actual.IdProyecto);
                if (pendientes > 0)
                    ModelState.AddModelError(nameof(m.IdEstado), $"No se puede finalizar el proyecto: hay {pendientes} tarea(s) sin completar.");
            }
        }
    }

    private async Task PrepararFormulario(ProyectoViewModel m)
    {
        var cliente = m.IdCliente;
        var categoria = m.IdCategoria;
        var responsable = m.IdResponsable;

        ViewBag.Clientes = new SelectList(
            await db.Clientes.AsNoTracking().Where(c => c.Estado || c.IdCliente == cliente).OrderBy(c => c.NombreCliente).ToListAsync(),
            "IdCliente", "NombreCliente", m.IdCliente);
        ViewBag.Categorias = new SelectList(
            await db.Categorias.AsNoTracking().Where(c => c.Estado || c.IdCategoria == categoria).OrderBy(c => c.Nombre).ToListAsync(),
            "IdCategoria", "Nombre", m.IdCategoria);
        ViewBag.Estados = new SelectList(
            await db.Estados.AsNoTracking().Where(e => e.TipoEstado == "PROYECTO").OrderBy(e => e.IdEstado).ToListAsync(),
            "IdEstado", "Nombre", m.IdEstado);
        ViewBag.Responsables = new SelectList(
            await db.Usuarios.AsNoTracking().Where(u => u.Estado || u.IdUsuario == responsable).OrderBy(u => u.Nombre)
                .Select(u => new { u.IdUsuario, Texto = u.Nombre + " " + u.Apellido1 }).ToListAsync(),
            "IdUsuario", "Texto", m.IdResponsable);
    }
}
