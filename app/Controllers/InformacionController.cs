using Microsoft.AspNetCore.Mvc;
namespace Controlex.Web.Controllers;

/// <summary>Acerca de (RF-013) y Ayuda (RF-014): disponibles para todos los roles.</summary>
public class InformacionController : Controller
{
    public IActionResult AcercaDe() => View();
    public IActionResult Ayuda() => View();
}
