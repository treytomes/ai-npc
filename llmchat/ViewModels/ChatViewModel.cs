using System;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace llmchat.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
	private readonly ChatHistory _history;

	public ChatViewModel(ChatHistory history)
		: base()
	{
		_history = history ?? throw new ArgumentNullException(nameof(history));

		Title = "A Sample Chat";
		History = new ChatHistoryViewModel(_history);
		MessageInput = new MessageInputViewModel();

		MessageInput.SendRequested += OnSendRequested;
	}

	public string Title { get; }
	public ChatHistoryViewModel History { get; }
	public MessageInputViewModel MessageInput { get; }

	private async void OnSendRequested(string text)
	{
		// User message.
		var user = new ChatMessageContent(AuthorRole.User, text);
		_history.Add(user);
		History.AddMessage(user);

		// Sample assistant response.
		var assistant = new ChatMessageContent(
			AuthorRole.Assistant,
			$"You said: \"{text}\"");

		_history.Add(assistant);
		History.AddMessage(assistant);
	}
}
