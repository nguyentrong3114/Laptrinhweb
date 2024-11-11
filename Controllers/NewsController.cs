using Microsoft.AspNetCore.Mvc;

public class NewsController : Controller
{
    public IActionResult Index()
    {
        return View();
    }
}