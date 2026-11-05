using Microsoft.AspNetCore.Mvc;
namespace Controlex.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();
}
