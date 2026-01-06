using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace llmchat.ViewModels;

public sealed class ChatMessageViewModel : ViewModelBase
{
	private bool _isExpanded = true;

	public ChatMessageViewModel(ChatMessageContent message)
		: base()
	{
		Role = message.Role;
		Content = message.Content ?? string.Empty;
		ToggleExpandedCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
	}

	public bool IsExpanded
	{
		get => _isExpanded;
		set => SetProperty(ref _isExpanded, value);
	}

	public AuthorRole Role { get; }

	public string Content { get; }

	public string RoleLabel => Role.Label;

	public string RoleClass => RoleLabel switch
	{
		"user" => "role-user",
		"assistant" => "role-assistant",
		"system" => "role-system",
		"tool" => "role-tool",
		_ => "role-unknown"
	};

	public bool IsUser => Role == AuthorRole.User;
	public bool IsAssistant => Role == AuthorRole.Assistant;
	public bool IsSystem => Role == AuthorRole.System;
	public bool IsTool => Role == AuthorRole.Tool;

	public string ExpandGlyph => IsExpanded ? "▼" : "▶";

	public ICommand ToggleExpandedCommand { get; }
}
