using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

[RequiereModulo("CLIENTES", 1)]
public class ClientesController(ControlexDbContext contexto, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var total = await contexto.Clientes.CountAsync();
        var paginacion = Paginacion.Crear("Clientes", nameof(Index), pagina, total);
        var clientes = await contexto.Clientes.AsNoTracking().Include(cliente => cliente.TipoCliente)
            .OrderBy(cliente => cliente.NombreCliente).ThenBy(cliente => cliente.IdCliente)
            .Skip((paginacion.Pagina - 1) * paginacion.Tam).Take(paginacion.Tam).ToListAsync();
        return View(new ClientesIndexViewModel { Filas = clientes, Paginacion = paginacion });
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarTipos();
        return View("Formulario", new ClienteViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ClienteViewModel modeloCliente)
    {
        await ValidarUnicos(modeloCliente);
        if (!ModelState.IsValid) { await CargarTipos(); return View("Formulario", modeloCliente); }

        var cliente = new Cliente
        {
            IdTipoCliente = modeloCliente.IdTipoCliente, Identificacion = modeloCliente.Identificacion, NombreCliente = modeloCliente.NombreCliente.Trim(),
            Contacto = modeloCliente.Contacto?.Trim(), Telefono1 = modeloCliente.Telefono1, Telefono2 = modeloCliente.Telefono2,
            Correo = modeloCliente.Correo?.Trim(), Direccion = modeloCliente.Direccion?.Trim(), Estado = modeloCliente.Estado
        };
        contexto.Clientes.Add(cliente);
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "CLIENTES", "INSERCIÓN", $"Cliente creado: {cliente.NombreCliente}");

        TempData["Mensaje"] = "Cliente registrado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var cliente = await contexto.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();
        await CargarTipos();
        return View("Formulario", new ClienteViewModel
        {
            IdCliente = cliente.IdCliente, IdTipoCliente = cliente.IdTipoCliente, Identificacion = cliente.Identificacion,
            NombreCliente = cliente.NombreCliente, Contacto = cliente.Contacto, Telefono1 = cliente.Telefono1, Telefono2 = cliente.Telefono2,
            Correo = cliente.Correo, Direccion = cliente.Direccion, Estado = cliente.Estado
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ClienteViewModel modeloCliente)
    {
        modeloCliente.IdCliente = id;
        var cliente = await contexto.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();
        await ValidarUnicos(modeloCliente);
        if (!ModelState.IsValid) { await CargarTipos(); return View("Formulario", modeloCliente); }

        cliente.IdTipoCliente = modeloCliente.IdTipoCliente; cliente.Identificacion = modeloCliente.Identificacion; cliente.NombreCliente = modeloCliente.NombreCliente.Trim();
        cliente.Contacto = modeloCliente.Contacto?.Trim(); cliente.Telefono1 = modeloCliente.Telefono1; cliente.Telefono2 = modeloCliente.Telefono2;
        cliente.Correo = modeloCliente.Correo?.Trim(); cliente.Direccion = modeloCliente.Direccion?.Trim(); cliente.Estado = modeloCliente.Estado;
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "CLIENTES", "MODIFICACIÓN", $"Cliente modificado: {cliente.NombreCliente}");

        TempData["Mensaje"] = "Cliente actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var cliente = await contexto.Clientes.FindAsync(id);
        if (cliente is null) return NotFound();
        cliente.Estado = !cliente.Estado;
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "CLIENTES", cliente.Estado ? "HABILITACIÓN" : "INHABILITACIÓN", $"Cliente: {cliente.NombreCliente}");
        TempData["Mensaje"] = cliente.Estado ? "Cliente habilitado." : "Cliente inhabilitado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarUnicos(ClienteViewModel modeloCliente)
    {
        if (await contexto.Clientes.AnyAsync(cliente =>
                cliente.Identificacion == modeloCliente.Identificacion && cliente.IdCliente != modeloCliente.IdCliente))
            ModelState.AddModelError(nameof(modeloCliente.Identificacion), "Ya existe un cliente con esa identificación.");
        if (modeloCliente.IdTipoCliente > 0 &&
            !await contexto.TiposCliente.AnyAsync(tipo => tipo.IdTipoCliente == modeloCliente.IdTipoCliente))
            ModelState.AddModelError(nameof(modeloCliente.IdTipoCliente), "Seleccione un tipo de cliente válido.");
    }

    private async Task CargarTipos() =>
        ViewBag.Tipos = new SelectList(await contexto.TiposCliente.OrderBy(tipo => tipo.Nombre).ToListAsync(), "IdTipoCliente", "Nombre");
}
