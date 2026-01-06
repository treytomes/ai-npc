using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace llmchat.ViewModels;

public sealed class ChatHistoryViewModel : ViewModelBase
{
	public ObservableCollection<ChatMessageViewModel> Messages { get; }
	private string? _toastMessage;
	private bool _isToastVisible;

	public ChatHistoryViewModel(ChatHistory history)
		: base()
	{
		Messages = new ObservableCollection<ChatMessageViewModel>(history.Select(m =>
		{
			var vm = new ChatMessageViewModel(m);
			vm.DeleteRequested += OnDeleteRequested;
			vm.ToastRequested += ShowToast;
			return vm;
		}));

		System.Console.WriteLine($"Messages loaded: {Messages.Count}");

	}

	public string? ToastMessage
	{
		get => _toastMessage;
		set => SetProperty(ref _toastMessage, value);
	}

	public bool IsToastVisible
	{
		get => _isToastVisible;
		private set => SetProperty(ref _isToastVisible, value);
	}

	public async void ShowToast(string message)
	{
		ToastMessage = message;
		IsToastVisible = true;
		await Task.Delay(1500);
		ToastMessage = null;
		IsToastVisible = false;
	}

	private void OnDeleteRequested(ChatMessageViewModel message)
	{
		Messages.Remove(message);
	}

	// Sample preload factory.
	public static ChatHistoryViewModel CreateSample()
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

		return new ChatHistoryViewModel(history);
	}
}
