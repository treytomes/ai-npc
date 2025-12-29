using Microsoft.Extensions.AI;

namespace Adventure.LLM.Services;

public interface ILLMManager : IDisposable
{
	Task<IChatClient> CreateChatClient();
	Task InitializeAsync();
	Task SetModelAsync(string modelName);
}
