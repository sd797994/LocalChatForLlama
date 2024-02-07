using LLama.Common;
using System.Net.WebSockets;
using System.Text;

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
                await foreach (var data in CustService.ChatAsync(context.Request.Query["session"].ToString(), receivedMessage))
                {
                    var responseData = Encoding.UTF8.GetBytes(data);
                    await webSocket.SendAsync(new ArraySegment<byte>(responseData, 0, responseData.Length), WebSocketMessageType.Text, true, CancellationToken.None);
                }
                result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            }
            await webSocket.CloseAsync(result.CloseStatus.Value, result.CloseStatusDescription, CancellationToken.None);

        }
    }
}
