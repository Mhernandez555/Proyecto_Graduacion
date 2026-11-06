using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

[RequiereModulo("CLIENTES", 1)]
public class ClientesController(ControlexDbContext db, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index() =>
        View(await db.Clientes.Include(c => c.TipoCliente).OrderBy(c => c.NombreCliente).ToListAsync());

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarTipos();
        return View("Formulario", new ClienteViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ClienteViewModel m)
    {
        await ValidarUnicos(m);
        if (!ModelState.IsValid) { await CargarTipos(); return View("Formulario", m); }

        var c = new Cliente
        {
            IdTipoCliente = m.IdTipoCliente, Identificacion = m.Identificacion, NombreCliente = m.NombreCliente.Trim(),
            Contacto = m.Contacto?.Trim(), Telefono1 = m.Telefono1, Telefono2 = m.Telefono2,
            Correo = m.Correo?.Trim(), Direccion = m.Direccion?.Trim(), Estado = m.Estado
        };
        db.Clientes.Add(c);
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "CLIENTES", "INSERCIÓN", $"Cliente creado: {c.NombreCliente}");

        TempData["Mensaje"] = "Cliente registrado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return NotFound();
        await CargarTipos();
        return View("Formulario", new ClienteViewModel
        {
            IdCliente = c.IdCliente, IdTipoCliente = c.IdTipoCliente, Identificacion = c.Identificacion,
            NombreCliente = c.NombreCliente, Contacto = c.Contacto, Telefono1 = c.Telefono1, Telefono2 = c.Telefono2,
            Correo = c.Correo, Direccion = c.Direccion, Estado = c.Estado
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ClienteViewModel m)
    {
        m.IdCliente = id;
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return NotFound();
        await ValidarUnicos(m);
        if (!ModelState.IsValid) { await CargarTipos(); return View("Formulario", m); }

        c.IdTipoCliente = m.IdTipoCliente; c.Identificacion = m.Identificacion; c.NombreCliente = m.NombreCliente.Trim();
        c.Contacto = m.Contacto?.Trim(); c.Telefono1 = m.Telefono1; c.Telefono2 = m.Telefono2;
        c.Correo = m.Correo?.Trim(); c.Direccion = m.Direccion?.Trim(); c.Estado = m.Estado;
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "CLIENTES", "MODIFICACIÓN", $"Cliente modificado: {c.NombreCliente}");

        TempData["Mensaje"] = "Cliente actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var c = await db.Clientes.FindAsync(id);
        if (c is null) return NotFound();
        c.Estado = !c.Estado;
        await db.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "CLIENTES", c.Estado ? "HABILITACIÓN" : "INHABILITACIÓN", $"Cliente: {c.NombreCliente}");
        TempData["Mensaje"] = c.Estado ? "Cliente habilitado." : "Cliente inhabilitado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarUnicos(ClienteViewModel m)
    {
        if (await db.Clientes.AnyAsync(x => x.Identificacion == m.Identificacion && x.IdCliente != m.IdCliente))
            ModelState.AddModelError(nameof(m.Identificacion), "Ya existe un cliente con esa identificación.");
        if (m.IdTipoCliente > 0 && !await db.TiposCliente.AnyAsync(t => t.IdTipoCliente == m.IdTipoCliente))
            ModelState.AddModelError(nameof(m.IdTipoCliente), "Seleccione un tipo de cliente válido.");
    }

    private async Task CargarTipos() =>
        ViewBag.Tipos = new SelectList(await db.TiposCliente.OrderBy(t => t.Nombre).ToListAsync(), "IdTipoCliente", "Nombre");
}
