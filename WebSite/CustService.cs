
using LLama;
using LLama.Common;
using LLamaSharp.KernelMemory;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.ContentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using System.Collections.Concurrent;
using System.Text;
using WebSite.Common;

namespace WebSite
{
    public class CustService : IHostedService
    {
        static string modelPath = "C:\\Users\\Administrator\\source\\repos\\LocalChatForLlama\\Llama3-8B-Chinese-Chat.q4_k_m.GGUF"; //定位到你的".gguf"对话模型所在位置;
        static string mmpmodelPath = "C:\\Users\\Administrator\\source\\repos\\LocalChatForLlama\\mmproj-mistral7b-f16-q6_k.gguf"; //定位到你的".gguf"CLIP模型所在位置;
        //对话模型：mistral-7b-evol-instruct-chinese.Q4_K_M.gguf 下载地址：https://huggingface.co/s3nh/Mistral-7B-Evol-Instruct-Chinese-GGUF/blob/main/mistral-7b-evol-instruct-chinese.Q4_K_M.gguf
        //多模态模型：llava-v1.6-mistral-7b.Q4_K_M.gguf 下载地址：https://huggingface.co/mradermacher/llava-v1.6-mistral-7b-GGUF/resolve/main/llava-v1.6-mistral-7b.Q4_K_M.gguf
        //CLIP模型：mmproj-mistral7b-f16-q6_k.gguf 下载地址：https://huggingface.co/cmp-nct/llava-1.6-gguf/resolve/main/mmproj-mistral7b-f16-q6_k.gguf
        static ConcurrentDictionary<string, (ChatSession, SemaphoreSlim)> sessionDic = new ConcurrentDictionary<string, (ChatSession, SemaphoreSlim)>();
        static LLamaWeights weights;
        static LLavaWeights clipWeights;
        static ModelParams modelParams;
        static IKernelMemory kernelMemory;
        const string storagefolder = "C:\\Users\\Administrator\\source\\repos\\LocalChatForLlama\\storage";
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            modelParams = new ModelParams(modelPath)
            {
                ContextSize = 4096
            };
            weights = LLamaWeights.LoadFromFile(modelParams);
            kernelMemory = CreateMemory(modelPath);
            clipWeights = LLavaWeights.LoadFromFile(mmpmodelPath);
            await Task.CompletedTask;
        }
        static (ChatSession session,SemaphoreSlim semaphore) GetSessionById(string id)
        {
            if (!sessionDic.ContainsKey(id))
            {
                sessionDic.TryAdd(id, (new ChatSession(new InteractiveExecutor(weights.CreateContext(modelParams))),new SemaphoreSlim(1)));
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
                    if (receive.Upfile[0].Split(".")[1].ToLower() == "pdf")
                    {
                        await kernelMemory.ImportDocumentAsync($"wwwroot{receive.Upfile[0]}", steps: Constants.PipelineWithoutSummary);
                    }
                    else
                    {
                        chatSession.session.Executor.ImagePaths.Add($"wwwroot{receive.Upfile[0]}");
                    }
                }
                if (receive.Upfile.Length == 0 || (receive.Upfile.Length > 0 && receive.Upfile[0].Split(".")[1].ToLower() != "pdf"))
                {
                    var aiResponse = chatSession.session.ChatAsync(new ChatHistory.Message(AuthorRole.User, $"{(receive.Upfile.Length > 0 ? "<image>\n" : "")}User:{receive.Message}\nAssistant:"), new InferenceParams { AntiPrompts = [antiPrompt], Temperature = 0.7f });
                    StringBuilder sb = new StringBuilder();
                    await foreach (var response in aiResponse)
                    {
                        sb.Append(response);
                        yield return response;
                    }
                }
                else if (receive.Upfile.Length > 0)
                {
                    MemoryAnswer answer = await kernelMemory.AskAsync(receive.Message);
                    yield return answer.Result;
                    yield return antiPrompt;
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
        public static IKernelMemory CreateMemory(string modelPath)
        {
            if(!Directory.Exists(storagefolder))
            {
                Directory.CreateDirectory(storagefolder);
            }
            var infParams = new InferenceParams() { AntiPrompts = [TextProcessor.wsend] };

            LLamaSharpConfig lsConfig = new(modelPath) { DefaultInferenceParams = infParams };

            SearchClientConfig searchClientConfig = new()
            {
                MaxMatchesCount = 3,
                AnswerTokens = 512,
            };

            TextPartitioningOptions parseOptions = new()
            {
                MaxTokensPerParagraph = 300,
                MaxTokensPerLine = 100,
                OverlappingTokens = 30
            };
            SimpleFileStorageConfig storageConfig = new()
            {
                Directory = storagefolder,
                StorageType = FileSystemTypes.Disk,
            };

            SimpleVectorDbConfig vectorDbConfig = new()
            {
                Directory = storagefolder,
                StorageType = FileSystemTypes.Disk,
            };
            return new KernelMemoryBuilder()
            .WithSimpleFileStorage(storageConfig)
            .WithSimpleVectorDb(vectorDbConfig)
                .WithLLamaSharpDefaults(lsConfig)
                .WithSearchClientConfig(searchClientConfig)
                .With(parseOptions)
                .Build();
        }
    }
    public class ReceiveDto
    {
        public string Session { get; set; }
        public string Message { get; set; }
        public string[] Upfile { get; set; }
    }
}
