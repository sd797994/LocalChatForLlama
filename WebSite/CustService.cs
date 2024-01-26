
using LLama.Common;
using LLama;

namespace WebSite
{
    public class CustService : IHostedService
    {
        string modelPath = //定位到你的".gguf"模型所在位置;
        public static ChatSession session;
        public static LLamaContext context;
        public static InteractiveExecutor ex;
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
            ex = new InteractiveExecutor(context);
            session = new ChatSession(ex);
            await Task.CompletedTask;
        }
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            await Task.CompletedTask;
        }
    }
}
