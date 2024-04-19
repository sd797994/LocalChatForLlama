
using LLama;
using LLama.Abstractions;
using LLama.Common;
using System.Collections.Concurrent;
using System.Text;
using static LLama.LLamaTransforms;
using static System.Net.Mime.MediaTypeNames;

namespace WebSite
{
    public class CustService : IHostedService
    {
        static string modelPath = "C:\\Users\\Administrator\\source\\repos\\LocalChatForLlama\\llava-v1.6-mistral-7b.Q4_K_M.gguf"; //定位到你的".gguf"对话模型所在位置;
        static string mmpmodelPath = "C:\\Users\\Administrator\\source\\repos\\LocalChatForLlama\\mmproj-mistral7b-f16-q6_k.gguf"; //定位到你的".gguf"CLIP模型所在位置;
        //mistral-7b-evol-instruct-chinese.Q4_K_M.gguf 下载地址：https://huggingface.co/s3nh/Mistral-7B-Evol-Instruct-Chinese-GGUF/blob/main/mistral-7b-evol-instruct-chinese.Q4_K_M.gguf
        //mmproj-mistral7b-f16-q6_k.gguf 下载地址：https://huggingface.co/cmp-nct/llava-1.6-gguf/resolve/main/mmproj-mistral7b-f16-q6_k.gguf?download=true
        static ConcurrentDictionary<string, (ChatSession, SemaphoreSlim)> sessionDic = new ConcurrentDictionary<string, (ChatSession, SemaphoreSlim)>();
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
        static (ChatSession session,SemaphoreSlim semaphore) GetSessionById(string id)
        {
            var param = new ModelParams(modelPath)
            {
                ContextSize = 4096
            };
            if (!sessionDic.ContainsKey(id))
            {
                sessionDic.TryAdd(id, (new ChatSession(new InteractiveExecutor(LLamaWeights.LoadFromFile(param).CreateContext(param), LLavaWeights.LoadFromFile(mmpmodelPath))),new SemaphoreSlim(1)));
            }
            return sessionDic[id];
        }
        public static async IAsyncEnumerable<string> ChatAsync(ReceiveDto receive, string antiPrompt)
        {
            var chatSession = GetSessionById(receive.Session);
            try
            {
                chatSession.semaphore.Wait();
                if (receive.Upfile.Length > 0)
                {
                    chatSession.session.Executor.ImagePaths.Add($"wwwroot{receive.Upfile[0]}");
                }
                var aiResponse = chatSession.session.ChatAsync(new ChatHistory.Message(AuthorRole.User, $"{(receive.Upfile.Length > 0?"<image>\n":"")}User:{receive.Message}\nAssistant:"), new InferenceParams { AntiPrompts = [antiPrompt], Temperature = 0.1f });
                StringBuilder sb = new StringBuilder();
                await foreach (var response in aiResponse)
                {
                    sb.Append(response);
                    yield return response;
                }
            }
            finally
            {
                chatSession.semaphore.Release();
            }
        }
        public static async Task<string> ChatAllAsync(string message, string antiPrompt)
        {
            var chatSession = GetSessionById(Guid.NewGuid().ToString());
            try
            {
                chatSession.semaphore.Wait();
                var history = new ChatHistory();
                history.AddMessage(AuthorRole.User, $"User:{message}\nAssistant:");
                var response = chatSession.session.ChatAsync(history, new InferenceParams { AntiPrompts = [antiPrompt], Temperature = 0.1f });
                StringBuilder sb = new StringBuilder();
                await foreach (var item in response)
                {
                    sb.Append(item);
                }
                return sb.ToString();
            }
            finally
            {
                chatSession.semaphore.Release();
            }
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
    public class ReceiveDto
    {
        public string Session { get; set; }
        public string Message { get; set; }
        public string[] Upfile { get; set; }
    }
}
