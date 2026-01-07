using System;
using Avalonia.Threading;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace llmchat.ViewModels;

public partial class ChatViewModel : ViewModelBase
{
	#region Fields

	private readonly Kernel _kernel;
	private readonly ChatHistory _history;

	#endregion

	#region Constructors

	public ChatViewModel(Kernel kernel, ChatHistory history)
		: base()
	{
		_kernel = kernel;
		_history = history ?? throw new ArgumentNullException(nameof(history));

		Title = "A Sample Chat";
		History = new ChatHistoryViewModel(_history);
		MessageInput = new MessageInputViewModel();

		MessageInput.SendRequested += OnSendRequested;
	}

	#endregion

	#region Properties

	public string Title { get; }
	public ChatHistoryViewModel History { get; }
	public MessageInputViewModel MessageInput { get; }

	#endregion

	#region Methods

	private async void OnSendRequested(string text)
	{
		if (!App.IsKernelReady && string.IsNullOrWhiteSpace(text))
		{
			return;
		}

		// User message
		var user = new ChatMessageContent(AuthorRole.User, text);
		_history.Add(user);
		History.AddMessage(user);

		// Create assistant VM ONLY (not in history yet)
		var assistantVm = new ChatMessageViewModel(
			new ChatMessageContent(AuthorRole.Assistant, "")
		);

		History.Messages.Add(assistantVm);

		try
		{
			var chatService = _kernel.GetRequiredService<IChatCompletionService>();

			await foreach (var chunk in chatService.GetStreamingChatMessageContentsAsync(_history))
			{
				if (!string.IsNullOrEmpty(chunk.Content))
				{
					Dispatcher.UIThread.Post(() =>
					{
						assistantVm.Append(chunk.Content);
					});
				}
			}

			// ✅ NOW add completed assistant message to history
			_history.Add(new ChatMessageContent(
				AuthorRole.Assistant,
				assistantVm.Content
			));
		}
		catch (Exception ex)
		{
			assistantVm.Append($"\n\nError: {ex.Message}");
		}
	}

	#endregion;
}
