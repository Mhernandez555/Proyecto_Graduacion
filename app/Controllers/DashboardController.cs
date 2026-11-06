using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace Controlex.Web.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index() => View();

    [AllowAnonymous]
    public IActionResult Error() => Content("Ocurrió un error inesperado. Intente nuevamente.");
}
