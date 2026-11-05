using Microsoft.AspNetCore.Mvc.Razor;
namespace Controlex.Web.Services;

/// <summary>Indica a Razor que las vistas están en app/Views/&lt;controlador&gt;/ y app/Views/shared/.</summary>
public class AppViewLocationExpander : IViewLocationExpander
{
    public void PopulateValues(ViewLocationExpanderContext context) { }

    public IEnumerable<string> ExpandViewLocations(ViewLocationExpanderContext context, IEnumerable<string> viewLocations)
    {
        var carpeta = (context.ControllerName ?? "").ToLowerInvariant();
        return [$"/app/Views/{carpeta}/{{0}}.cshtml", "/app/Views/shared/{0}.cshtml"];
    }
}
