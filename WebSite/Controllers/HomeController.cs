using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WebSite.Models;

namespace WebSite.Controllers
{
    public class HomeController : Controller
    {
        [HttpGet("")]
        [HttpGet("{param}")]
        public IActionResult Index()
        {
            return View();
        }
    }
}
