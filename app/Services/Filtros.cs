using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace Controlex.Web.Services;

/// <summary>Exige un nivel mínimo de acceso al módulo (0 sin acceso, 1 parcial, 2 total). Uso: [RequiereModulo("USUARIOS", 1)]</summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class RequiereModuloAttribute(string modulo, int nivelMinimo = 1) : Attribute, IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext contexto)
    {
        var valor = contexto.HttpContext.User.FindFirst("perm:" + modulo)?.Value;
        if (!int.TryParse(valor, out var nivel) || nivel < nivelMinimo)
            contexto.Result = new ForbidResult();
    }
}

/// <summary>Si el usuario tiene contraseña temporal, lo envía a cambiarla antes de usar el sistema.</summary>
public class CambioContrasenaObligatorioFilter : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext contexto)
    {
        var usuario = contexto.HttpContext.User;
        if (usuario.Identity?.IsAuthenticated != true || usuario.FindFirst("requiere_cambio")?.Value != "True") return;

        var controlador = contexto.RouteData.Values["controller"]?.ToString();
        if (string.Equals(controlador, "Seguridad", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(controlador, "Auth", StringComparison.OrdinalIgnoreCase)) return;

        contexto.Result = new RedirectToActionResult("CambiarContrasena", "Seguridad", null);
    }

    public void OnActionExecuted(ActionExecutedContext contexto) { }
}
