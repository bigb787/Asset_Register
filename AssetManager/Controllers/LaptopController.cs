using Microsoft.AspNetCore.Mvc;

namespace AssetManager.Controllers;

public class LaptopController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Laptop";
        return View("~/Views/Home/Index.cshtml");
    }
}
