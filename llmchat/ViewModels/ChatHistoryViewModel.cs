using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.ObjectModel;
using System.Linq;

namespace llmchat.ViewModels;

public sealed class ChatHistoryViewModel : ViewModelBase
{
	public ObservableCollection<ChatMessageViewModel> Messages { get; }

	public ChatHistoryViewModel(ChatHistory history)
		: base()
	{
		Messages = new ObservableCollection<ChatMessageViewModel>(
		history.Select(m => new ChatMessageViewModel(m)));

		System.Console.WriteLine($"Messages loaded: {Messages.Count}");

	}

	// Sample preload factory
	public static ChatHistoryViewModel CreateSample()
	{
		var history = new ChatHistory();

		history.AddSystemMessage(
			"You are an experimental assistant embedded in a desktop LLM lab.");

		history.AddUserMessage(
			"Hello. Can you explain what this app is for?");

		history.AddAssistantMessage(
			"This application is a desktop environment for experimenting with local and remote language models.");

		return new ChatHistoryViewModel(history);
	}
}
