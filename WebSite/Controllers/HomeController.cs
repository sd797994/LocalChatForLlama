using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebSite.Models;

namespace WebSite.Controllers
{
    public class HomeController : Controller
    {
        
        public IActionResult Index()
        {
            return View();
        }
    }
}
