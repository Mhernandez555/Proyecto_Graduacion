<<<<<<< HEAD
using Controlex.Web.Data;
=======
using Controlex.Web.Models;
>>>>>>> 62c5efa3020e94f6d5358771d33d3b88e3bcae43
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Services;

public class PermisoService(ControlexDbContext db)
{
    public static readonly string[] Modulos =
        ["PROYECTOS", "TAREAS", "REP_RENTABILIDAD", "REP_TAREAS", "BITACORAS", "USUARIOS", "CLIENTES", "ROLES_PERMISOS"];

<<<<<<< HEAD
    /// <summary>Nivel de acceso por módulo. El rol administrador prevalece (siempre 2).</summary>
=======
    /// <summary>Nivel de acceso por módulo (0 sin acceso, 1 parcial, 2 total). El rol administrador prevalece.</summary>
>>>>>>> 62c5efa3020e94f6d5358771d33d3b88e3bcae43
    public async Task<Dictionary<string, int>> NivelesAsync(Usuario u)
    {
        if (u.Rol.EsAdministrador) return Modulos.ToDictionary(m => m, _ => 2);
        var d = await db.Permisos.Where(p => p.IdRol == u.IdRol).ToDictionaryAsync(p => p.Modulo, p => (int)p.NivelAcceso);
        return Modulos.ToDictionary(m => m, m => d.GetValueOrDefault(m, 0));
    }
}
