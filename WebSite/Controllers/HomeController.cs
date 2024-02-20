using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using WebSite.Common;
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

        [HttpGet("/createtitle")]
        public async Task<string> CreateTitle(string content)
        {
            var result = await CustService.ChatAllAsync(content, TextProcessor.wsend);
            return result.Replace(TextProcessor.wsend, "").Replace("\"", "").Replace("\n", "").Replace("±ÍÃ‚£∫", "");
        }
    }
}
