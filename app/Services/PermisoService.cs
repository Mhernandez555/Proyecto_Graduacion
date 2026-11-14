using Controlex.Web.Models;
using Microsoft.EntityFrameworkCore;
namespace Controlex.Web.Services;

public class PermisoService(ControlexDbContext contexto)
{
    public static readonly string[] Modulos =
        ["PROYECTOS", "TAREAS", "REP_RENTABILIDAD", "REP_TAREAS", "BITACORAS", "USUARIOS", "CLIENTES", "ROLES_PERMISOS"];


    /// <summary>Nivel de acceso por módulo (0 sin acceso, 1 parcial, 2 total). El rol administrador prevalece.</summary>
    public async Task<Dictionary<string, int>> NivelesAsync(Usuario usuario)
    {
        if (usuario.Rol.EsAdministrador) return Modulos.ToDictionary(modulo => modulo, _ => 2);
        var nivelesPorModulo = await contexto.Permisos.Where(permiso => permiso.IdRol == usuario.IdRol)
            .ToDictionaryAsync(permiso => permiso.Modulo, permiso => (int)permiso.NivelAcceso);
        return Modulos.ToDictionary(modulo => modulo, modulo => nivelesPorModulo.GetValueOrDefault(modulo, 0));
    }
}
