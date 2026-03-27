using Microsoft.AspNetCore.Mvc;

namespace AssetManager.Controllers;

public class RegistersController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Registers";
        return View();
    }
}
