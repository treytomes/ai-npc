using CommunityToolkit.Mvvm.Input;
using System.Windows.Input;

namespace llmchat.ViewModels;

public sealed class MessageInputViewModel : ViewModelBase
{
	private string _text = string.Empty;

	public MessageInputViewModel()
	{
		SendCommand = new RelayCommand(Send, () => CanSend);
	}

	public string Text
	{
		get => _text;
		set => SetProperty(ref _text, value);
	}

	public ICommand SendCommand { get; }
	public bool CanSend => !string.IsNullOrWhiteSpace(Text);

	private void Send()
	{
		if (string.IsNullOrWhiteSpace(Text))
			return;

		// Later: emit event / add to chat history
		Text = string.Empty;
	}
}
