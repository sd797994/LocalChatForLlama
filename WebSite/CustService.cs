
using LLama.Common;
using LLama;
using System.Net.WebSockets;
using System.Text;
using System.Collections.Concurrent;
using System.Reflection.Metadata;
using Microsoft.AspNetCore.Http;

namespace WebSite
{
    public class CustService : IHostedService
    {
        string modelPath = "C:\\Users\\Administrator\\source\\repos\\LocalChatForLlama\\mistral-7b-evol-instruct-chinese.Q5_K_M.gguf"; //定位到你的".gguf"模型所在位置;
        //mistral-7b-evol-instruct-chinese.Q5_K_M.gguf 下载地址：https://huggingface.co/s3nh/Mistral-7B-Evol-Instruct-Chinese-GGUF/blob/main/mistral-7b-evol-instruct-chinese.Q5_K_M.gguf
        public static LLamaContext context;
        public static LLamaContext context2;
        public static ChatSession session2;
        public static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(1);
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
            var model2 = LLamaWeights.LoadFromFile(parameters);
            context2 = model2.CreateContext(parameters);
            session2 = new ChatSession(new InteractiveExecutor(context2));
            await Task.CompletedTask;
        }
        static ChatSession GetSessionById(string id)
        {
            if (!sessionDic.ContainsKey(id))
                sessionDic.TryAdd(id, new ChatSession(new InteractiveExecutor(context)));
            return sessionDic[id];
        }
        public static async IAsyncEnumerable<string> ChatAsync(string session,string message, string antiPrompt)
        {
            var aiResponse = GetSessionById(session).ChatAsync(new ChatHistory.Message(AuthorRole.User, $"[INST]###USER:\n{message}\n###ASSISTANT:\n[/INST]"), new InferenceParams { Temperature = 0.6f, AntiPrompts = [antiPrompt] });
            await foreach (var response in aiResponse)
            {
                yield return response;
            }
        }
        public static async Task<string> ChatAllAsync(string message, string antiPrompt)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                session2.History.Messages.Clear();
                var response = session2.ChatAsync(new ChatHistory.Message(AuthorRole.User, $"Instruct:{message}\nOutput:"), new InferenceParams { Temperature = 0.3f, AntiPrompts = [antiPrompt] });
                StringBuilder sb = new StringBuilder();
                await foreach (var item in response)
                {
                    sb.Append(item);
                }
                return sb.ToString();
            }
            finally
            {
                semaphoreSlim.Release();
            }
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
