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
            StringBuilder sb = new StringBuilder();
            await foreach (var data in CustService.ChatAsync(Guid.NewGuid().ToString(), content, TextProcessor.wsend))
            {
                sb.Append(data);
            }
            return sb.ToString().Replace(TextProcessor.wsend, "").Replace("\"", "").Replace("\n", "");
        }
    }
}
