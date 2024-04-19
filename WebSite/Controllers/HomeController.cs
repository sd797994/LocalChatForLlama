using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
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
            return result.Replace(TextProcessor.wsend, "").Replace("\"", "").Replace("\n", "").Replace("标题：", "");
        }
        [HttpGet("/ws/{param}")]
        public async Task ChatByWs()
        {
            if (HttpContext.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await HttpContext.WebSockets.AcceptWebSocketAsync();
                var buffer = new byte[1024 * 4];
                var receivedData = new List<byte>();
                string receivedMessage = "";
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    receivedData.AddRange(new ArraySegment<byte>(buffer, 0, result.Count));
                    if (result.EndOfMessage)
                    {
                        receivedMessage = Encoding.UTF8.GetString(receivedData.ToArray(), 0, receivedData.Count);
                        receivedData.Clear();
                        StringBuilder sb = new StringBuilder();
                        if(!string.IsNullOrWhiteSpace(receivedMessage))
                        {
                            var receive = JsonSerializer.Deserialize<ReceiveDto>(receivedMessage, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                            await foreach (var data in CustService.ChatAsync(receive, TextProcessor.wsend))
                            {
                                sb.Append(data);
                                if (TextProcessor.CheckEnd(sb))
                                {
                                    var responseData = Encoding.UTF8.GetBytes(sb.ToString());
                                    await webSocket.SendAsync(new ArraySegment<byte>(responseData, 0, responseData.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                                    sb.Clear();
                                }
                            }
                        }
                    }
                } while (!result.CloseStatus.HasValue);
                await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
            }
        }
        [HttpPost("/uploadfile")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }
            var allowedTypes = new[] { "image/jpeg", "image/png", "application/pdf", "application/vnd.openxmlformats-officedocument.wordprocessingml.document" };
            if (!Array.Exists(allowedTypes, type => type == file.ContentType))
            {
                return BadRequest("只接受 JPEG, PNG, PDF, DOCX文件");
            }
            var fileExtension = Path.GetExtension(file.FileName);
            var newFileName = $"{Guid.NewGuid()}{fileExtension}";
            if (!Directory.Exists("wwwroot/tmp"))
                Directory.CreateDirectory("wwwroot/tmp");
            var savePath = Path.Combine("wwwroot/tmp", newFileName);
            try
            {
                using (var stream = new FileStream(savePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                return Ok(new { url = $"/tmp/{newFileName}" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
