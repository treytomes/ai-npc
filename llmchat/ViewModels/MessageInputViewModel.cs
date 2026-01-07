using CommunityToolkit.Mvvm.Input;
using System;
using System.Windows.Input;

namespace llmchat.ViewModels;

public sealed class MessageInputViewModel : ViewModelBase
{
	/// Raised when user requests to send text
	public event Action<string>? SendRequested;

	private string _text = string.Empty;

	public MessageInputViewModel()
	{
		SendCommand = new RelayCommand(ExecuteSend, () => !string.IsNullOrWhiteSpace(Text));
	}

	public string Text
	{
		get => _text;
		set
		{
			if (SetProperty(ref _text, value))
			{
				(SendCommand as RelayCommand)!.NotifyCanExecuteChanged();
			}
		}
	}

	public ICommand SendCommand { get; }

	private void ExecuteSend()
	{
		var message = Text.Trim();
		Text = string.Empty;

		SendRequested?.Invoke(message);
	}
}