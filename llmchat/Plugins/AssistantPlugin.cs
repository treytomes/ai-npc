using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.ComponentModel;
using System.Threading.Tasks;

namespace llmchat.Plugins;

public sealed class AssistantPlugin
{
	private readonly IChatCompletionService _chat;

	public AssistantPlugin(Kernel kernel)
	{
		_chat = kernel.GetRequiredService<IChatCompletionService>();
	}

	[KernelFunction("Chat")]
	[Description("Responds as a helpful assistant")]
	public async Task<string> ChatAsync(
		[Description("Conversation history")] ChatHistory history)
	{
		var result = await _chat.GetChatMessageContentAsync(history);
		return result.Content ?? string.Empty;
	}
}
