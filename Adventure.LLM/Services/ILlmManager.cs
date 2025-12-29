using Microsoft.Extensions.AI;

namespace Adventure.LLM.Services;

public interface ILlmManager : IDisposable
{
	Task<IChatClient> CreateChatClient();
	Task InitializeAsync();
	void SetModel(string modelName);
}
