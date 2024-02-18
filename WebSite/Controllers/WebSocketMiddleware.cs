using LLama.Common;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using WebSite.Common;

namespace WebSite.Controllers
{
    public class WebSocketMiddleware
    {
        private readonly RequestDelegate _next;
        public WebSocketMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.WebSockets.IsWebSocketRequest)
            {
                using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                await Echo(context, webSocket);
            }
            else
            {
                await _next(context);
            }
        }
        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                StringBuilder sb = new StringBuilder();
                await foreach (var data in CustService.ChatAsync(context.Request.Path.ToString().Split("/")[1], receivedMessage, TextProcessor.wsend))
                {
                    sb.Append(data);
                    if (TextProcessor.CheckEnd(sb))
                    {
                        var responseData = Encoding.UTF8.GetBytes(sb.ToString());
                        await webSocket.SendAsync(new ArraySegment<byte>(responseData, 0, responseData.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                        sb.Clear();
                    }
                }
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);
        }
    }
}
