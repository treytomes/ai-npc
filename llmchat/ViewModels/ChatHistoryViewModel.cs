using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace llmchat.ViewModels;

public sealed class ChatHistoryViewModel : ViewModelBase
{
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
	}

	public ObservableCollection<ChatMessageViewModel> Messages { get; }

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

	public void AddMessage(ChatMessageContent message)
	{
		Messages.Add(new ChatMessageViewModel(message));
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
}
