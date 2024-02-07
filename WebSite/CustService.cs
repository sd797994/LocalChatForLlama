
using LLama.Common;
using LLama;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;

namespace WebSite
{
    public class CustService : IHostedService
    {
        string modelPath = "C:\\Users\\Administrator\\source\\repos\\LocalChatForLlama\\mistral-7b-evol-instruct-chinese.Q4_K_M.gguf"; //定位到你的".gguf"模型所在位置;
        public static ChatSession session;
        public static LLamaContext context;
        static ConcurrentDictionary<string, ChatSession> sessionDic = new ConcurrentDictionary<string, ChatSession>();
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var parameters = new ModelParams(modelPath)
            {
                ContextSize = 4096,
                Seed = 1337,
                GpuLayerCount = 5
            };
            var model = LLamaWeights.LoadFromFile(parameters);
            context = model.CreateContext(parameters);
            await Task.CompletedTask;
        }
        static ChatSession GetSessionById(string id)
        {
            if (!sessionDic.ContainsKey(id))
                sessionDic.TryAdd(id, new ChatSession(new InteractiveExecutor(context)));
            return sessionDic[id];
        }
        public static async IAsyncEnumerable<string> ChatAsync(string session,string message)
        {
            var aiResponse = GetSessionById(session).ChatAsync(new ChatHistory.Message(AuthorRole.User, $"USER:{message}\nASSITANT:"), new InferenceParams { Temperature = 0.7f, AntiPrompts = ["wsend"] });
            await foreach (var response in aiResponse)
            {
                yield return response;
            }
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
