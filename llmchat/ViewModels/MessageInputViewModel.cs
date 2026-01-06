using CommunityToolkit.Mvvm.Input;
using OllamaSharp.Models.Chat;
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
		SendCommand = new RelayCommand(ExecuteSend, CanExecuteSend);
	}

	public string Text
	{
		get => _text;
		set => SetProperty(ref _text, value);
	}

	public ICommand SendCommand { get; }
	public bool CanSend => !string.IsNullOrWhiteSpace(Text);

	private void ExecuteSend()
	{
		if (!CanSend) return;

		var message = Text.Trim();
		Text = string.Empty;

		SendRequested?.Invoke(message);
	}

	private bool CanExecuteSend() => CanSend;
}