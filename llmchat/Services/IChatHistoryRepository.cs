using Microsoft.SemanticKernel.ChatCompletion;

namespace llmchat.Services;

public interface IChatHistoryRepository
{
	ChatHistory CreateSample();
	ChatHistory CreateEmpty();
}
