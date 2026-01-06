using Microsoft.SemanticKernel.ChatCompletion;

namespace llmchat.Services;

public class ChatHistoryRepository : IChatHistoryRepository
{
	public ChatHistory CreateSample()
	{
		var history = new ChatHistory();

		history.AddSystemMessage(
			"You are an experimental assistant embedded in a desktop LLM lab.");

		for (int i = 0; i < 25; i++)
		{
			history.AddUserMessage(
				$"User message #{i + 1}: Can you explain concept #{i + 1}?");

			history.AddAssistantMessage(
				$"Assistant reply #{i + 1}: This is a longer response intended to " +
				$"simulate real chat output. It may span multiple lines and should " +
				$"wrap correctly in the UI. The purpose is to stress scrolling and " +
				$"virtualization behavior.");
		}

		return history;
	}
}