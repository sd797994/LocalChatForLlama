using LLama.Common;
using System.Linq;
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
        static string wsend = "wsend";
        private async Task Echo(HttpContext context, WebSocket webSocket)
        {
            var buffer = new byte[1024 * 4];
            WebSocketReceiveResult result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
            while (!result.CloseStatus.HasValue)
            {
                var receivedMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                StringBuilder sb = new StringBuilder();
                await foreach (var data in CustService.ChatAsync(context.Request.Path.ToString().Split("/")[1], receivedMessage, wsend))
                {
                    sb.Append(data);
                    if (CheckEnd(sb))
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
        bool CheckEnd(StringBuilder sb)
        {
            var content = sb.ToString();
            //如果不包含或者完全包含，直接返回
            var intersectStr = GetLongestCommonSubstring(content, wsend);
            if (intersectStr.Length == 0)
                return true;
            else
            {
                if (intersectStr == wsend || content.IndexOf(intersectStr) + intersectStr.Length != content.Length)
                {
                    return true;
                }
                else
                    return false;
            }
        }
        static string GetLongestCommonSubstring(string str1, string str2)
        {
            if (string.IsNullOrEmpty(str1) || string.IsNullOrEmpty(str2))
            {
                return "";
            }

            int[,] dp = new int[str1.Length + 1, str2.Length + 1];
            int maxLength = 0;
            int end = 0;

            for (int i = 1; i <= str1.Length; i++)
            {
                for (int j = 1; j <= str2.Length; j++)
                {
                    if (str1[i - 1] == str2[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                        if (dp[i, j] > maxLength)
                        {
                            maxLength = dp[i, j];
                            end = i;
                        }
                    }
                }
            }

            return str1.Substring(end - maxLength, maxLength);
        }
    }
}
