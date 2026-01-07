using System;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace llmchat.ViewModels;

public sealed class ChatMessageViewModel : ViewModelBase
{
	#region Events

	public event Action<string>? ToastRequested;
	public event Action<ChatMessageViewModel>? DeleteRequested;

	#endregion

	#region Fields

	private bool _isExpanded = true;
	private bool _isArchived;
	private bool _isEditing;
	private string _content;
	private string _editBuffer = string.Empty;

	#endregion

	#region Constructors

	public ChatMessageViewModel(ChatMessageContent message)
	{
		Role = message.Role;
		_content = message.Content ?? string.Empty;

		ToggleExpandedCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
		CopyCommand = new AsyncRelayCommand(CopyToClipboardAsync);
		DeleteCommand = new RelayCommand(() => DeleteRequested?.Invoke(this));
		ToggleArchiveCommand = new RelayCommand(() => IsArchived = !IsArchived);
		EditCommand = new RelayCommand(StartEdit);
		SaveEditCommand = new RelayCommand(SaveEdit);
		CancelEditCommand = new RelayCommand(CancelEdit);
	}

	#endregion

	#region Properties

	// ----- State -----

	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			SetProperty(ref _isExpanded, value);
			OnPropertyChanged(nameof(ExpandGlyph));
		}
	}

	public bool IsArchived
	{
		get => _isArchived;
		set => SetProperty(ref _isArchived, value);
	}

	public bool IsEditing
	{
		get => _isEditing;
		private set => SetProperty(ref _isEditing, value);
	}

	public AuthorRole Role { get; }

	public string Content
	{
		get => _content;
		private set => SetProperty(ref _content, value);
	}

	public string EditBuffer
	{
		get => _editBuffer;
		set => SetProperty(ref _editBuffer, value);
	}

	// ----- Derived -----

	public string RoleLabel => Role.Label;
	public string ExpandGlyph => IsExpanded ? "▼" : "▶";
	public bool IsUser => Role == AuthorRole.User;
	public bool IsAssistant => Role == AuthorRole.Assistant;
	public bool IsSystem => Role == AuthorRole.System;
	public bool IsTool => Role == AuthorRole.Tool;

	// ----- Commands -----

	public ICommand ToggleExpandedCommand { get; }
	public ICommand CopyCommand { get; }
	public ICommand DeleteCommand { get; }
	public ICommand ToggleArchiveCommand { get; }
	public ICommand EditCommand { get; }
	public ICommand SaveEditCommand { get; }
	public ICommand CancelEditCommand { get; }

	#endregion

	#region Methods

	public void Append(string text)
	{
		Content += text;
	}

	// ----- Command handlers -----

	private async Task CopyToClipboardAsync()
	{
		// TODO: I'd rather inject IClipboardService into the constructor, but this works.
		await App.ClipboardService.SetTextAsync(Content);
		ToastRequested?.Invoke("Copied to clipboard");
	}

	private void StartEdit()
	{
		EditBuffer = Content;
		IsEditing = true;
	}

	private void SaveEdit()
	{
		Content = EditBuffer;
		IsEditing = false;
	}

	private void CancelEdit()
	{
		EditBuffer = string.Empty;
		IsEditing = false;
	}

	#endregion
}
