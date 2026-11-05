using Microsoft.AspNetCore.Mvc;
namespace Controlex.Web.Controllers;

public class DashboardController : Controller
{
    public IActionResult Index() => View();
}
