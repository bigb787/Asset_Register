using Microsoft.AspNetCore.Mvc;

namespace AssetManager.Controllers;

public class HomeController : Controller
{
    public IActionResult Index() => View();

    public IActionResult Privacy() => View();
}
