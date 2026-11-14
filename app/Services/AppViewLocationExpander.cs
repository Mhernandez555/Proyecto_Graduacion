using Microsoft.AspNetCore.Mvc.Razor;
namespace Controlex.Web.Services;

/// <summary>Indica a Razor que las vistas están en app/Views/&lt;controlador&gt;/ y app/Views/shared/.</summary>
public class AppViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext contexto) { }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext contexto, IEnumerable<string> ubicacionesVista)
    {
        var carpetaControlador = (contexto.ControllerName ?? "").ToLowerInvariant();
        return [$"/app/Views/{carpetaControlador}/{{0}}.cshtml", "/app/Views/shared/{0}.cshtml"];
    }
}
