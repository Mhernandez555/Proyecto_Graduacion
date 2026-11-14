using System.Security.Claims;
using Controlex.Web.Models;
using Controlex.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Controllers;

[RequiereModulo("USUARIOS", 1)]
public class UsuariosController(ControlexDbContext contexto, BitacoraService bitacora) : Controller
{
    private int IdActual => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public async Task<IActionResult> Index(int pagina = 1)
    {
        var total = await contexto.Usuarios.CountAsync();
        var paginacion = Paginacion.Crear("Usuarios", nameof(Index), pagina, total);
        var usuarios = await contexto.Usuarios.AsNoTracking().Include(usuario => usuario.Rol)
            .OrderBy(usuario => usuario.Nombre).ThenBy(usuario => usuario.Apellido1).ThenBy(usuario => usuario.IdUsuario)
            .Skip((paginacion.Pagina - 1) * paginacion.Tam).Take(paginacion.Tam).ToListAsync();
        return View(new UsuariosIndexViewModel { Filas = usuarios, Paginacion = paginacion });
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        await CargarRoles();
        return View("Formulario", new UsuarioViewModel());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(UsuarioViewModel modeloUsuario)
    {
        if (string.IsNullOrEmpty(modeloUsuario.Contrasena)) ModelState.AddModelError(nameof(modeloUsuario.Contrasena), "Ingrese la contraseña.");
        else if (PasswordPolicy.Validar(modeloUsuario.Contrasena) is { } error)
            ModelState.AddModelError(nameof(modeloUsuario.Contrasena), error);
        await ValidarUnicos(modeloUsuario);
        if (!ModelState.IsValid) { await CargarRoles(); return View("Formulario", modeloUsuario); }

        var usuario = new Usuario
        {
            Cedula = modeloUsuario.Cedula, Nombre = modeloUsuario.Nombre.Trim(),
            Apellido1 = modeloUsuario.Apellido1.Trim(), Apellido2 = modeloUsuario.Apellido2?.Trim(),
            Correo = modeloUsuario.Correo.Trim(), IdRol = modeloUsuario.IdRol, NombreUsuario = modeloUsuario.NombreUsuario,
            ContrasenaHash = PasswordHasher.Hash(modeloUsuario.Contrasena!), RequiereCambioContrasena = true,
            CostoHora = modeloUsuario.CostoHora, Estado = modeloUsuario.Estado, FechaCreacion = DateTime.Now
        };
        contexto.Usuarios.Add(usuario);
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "USUARIOS", "INSERCIÓN", $"Usuario creado: {usuario.NombreUsuario}");

        TempData["Mensaje"] = "Usuario creado. Deberá cambiar su contraseña en el primer ingreso.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        var usuario = await contexto.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();
        await CargarRoles();
        return View("Formulario", new UsuarioViewModel
        {
            IdUsuario = usuario.IdUsuario, Cedula = usuario.Cedula, Nombre = usuario.Nombre,
            Apellido1 = usuario.Apellido1, Apellido2 = usuario.Apellido2,
            Correo = usuario.Correo, IdRol = usuario.IdRol, NombreUsuario = usuario.NombreUsuario,
            CostoHora = usuario.CostoHora, Estado = usuario.Estado
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, UsuarioViewModel modeloUsuario)
    {
        modeloUsuario.IdUsuario = id;
        var usuario = await contexto.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        var cambiaClave = !string.IsNullOrEmpty(modeloUsuario.Contrasena);
        if (cambiaClave && PasswordPolicy.Validar(modeloUsuario.Contrasena!) is { } error)
            ModelState.AddModelError(nameof(modeloUsuario.Contrasena), error);
        if (id == IdActual && !modeloUsuario.Estado)
            ModelState.AddModelError(nameof(modeloUsuario.Estado), "No puede inhabilitar su propio usuario.");
        await ValidarUnicos(modeloUsuario);
        if (!ModelState.IsValid) { await CargarRoles(); return View("Formulario", modeloUsuario); }

        usuario.Cedula = modeloUsuario.Cedula; usuario.Nombre = modeloUsuario.Nombre.Trim();
        usuario.Apellido1 = modeloUsuario.Apellido1.Trim(); usuario.Apellido2 = modeloUsuario.Apellido2?.Trim();
        usuario.Correo = modeloUsuario.Correo.Trim(); usuario.IdRol = modeloUsuario.IdRol; usuario.NombreUsuario = modeloUsuario.NombreUsuario;
        usuario.CostoHora = modeloUsuario.CostoHora; usuario.Estado = modeloUsuario.Estado;
        if (cambiaClave)
        {
            usuario.ContrasenaHash = PasswordHasher.Hash(modeloUsuario.Contrasena!);
            usuario.RequiereCambioContrasena = true;
        }
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "USUARIOS", "MODIFICACIÓN", $"Usuario modificado: {usuario.NombreUsuario}");

        TempData["Mensaje"] = "Usuario actualizado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id)
    {
        var usuario = await contexto.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();
        if (id == IdActual) { TempData["Error"] = "No puede inhabilitar su propio usuario."; return RedirectToAction(nameof(Index)); }

        usuario.Estado = !usuario.Estado;
        await contexto.SaveChangesAsync();
        await bitacora.MovimientoAsync(IdActual, "USUARIOS", usuario.Estado ? "HABILITACIÓN" : "INHABILITACIÓN", $"Usuario: {usuario.NombreUsuario}");
        TempData["Mensaje"] = usuario.Estado ? "Usuario habilitado." : "Usuario inhabilitado.";
        return RedirectToAction(nameof(Index));
    }

    private async Task ValidarUnicos(UsuarioViewModel modeloUsuario)
    {
        if (await contexto.Usuarios.AnyAsync(usuario =>
                usuario.Cedula == modeloUsuario.Cedula && usuario.IdUsuario != modeloUsuario.IdUsuario))
            ModelState.AddModelError(nameof(modeloUsuario.Cedula), "Ya existe un usuario con esa cédula.");
        if (await contexto.Usuarios.AnyAsync(usuario =>
                usuario.Correo == modeloUsuario.Correo && usuario.IdUsuario != modeloUsuario.IdUsuario))
            ModelState.AddModelError(nameof(modeloUsuario.Correo), "Ya existe un usuario con ese correo.");
        if (await contexto.Usuarios.AnyAsync(usuario =>
                usuario.NombreUsuario == modeloUsuario.NombreUsuario && usuario.IdUsuario != modeloUsuario.IdUsuario))
            ModelState.AddModelError(nameof(modeloUsuario.NombreUsuario), "Ese nombre de usuario ya está en uso.");
        if (modeloUsuario.IdRol > 0 &&
            !await contexto.Roles.AnyAsync(rol => rol.IdRol == modeloUsuario.IdRol && rol.Estado))
            ModelState.AddModelError(nameof(modeloUsuario.IdRol), "Seleccione un rol activo.");
    }

    private async Task CargarRoles() =>
        ViewBag.Roles = new SelectList(await contexto.Roles.Where(rol => rol.Estado)
            .OrderBy(rol => rol.NombreRol).ToListAsync(), "IdRol", "NombreRol");
}
