using Adventure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Terminal.Gui;

namespace llmchat;

internal sealed class MainAppState : AppState
{
	#region Fields

	private readonly ILogger<MainAppState> _logger;
	private readonly AppSettings _settings;
	private readonly TerminalGuiAppEngine _engine;
	private readonly ChatHistory _chatHistory;

	private Toplevel _top = null!;
	private Window _mainWindow = null!;
	private MenuBar _menuBar = null!;
	private StatusBar _statusBar = null!;

	// Chat UI components
	private ScrollView _chatScrollView = null!;
	private View _chatHistoryView = null!;
	private TextView _messageInput = null!;
	private Button _sendButton = null!;
	private int _messageCounter = 0;

	#endregion

	#region Constructors

	public MainAppState(
		IOptions<AppSettings> settings,
		IStateManager states,
		ILogger<MainAppState> logger,
		IAppEngine engine
	) : base(states)
	{
		_settings = settings.Value;
		_logger = logger;
		_engine = engine as TerminalGuiAppEngine ?? throw new InvalidOperationException("MainAppState requires TerminalGuiAppEngine");
		_chatHistory = new ChatHistory();
	}

	#endregion

	#region Methods

	public override async Task OnLoadAsync()
	{
		_logger.LogInformation("Loading MainAppState");

		// Initialize Terminal.Gui
		Application.Init();

		await Task.CompletedTask;
	}

	public override async Task OnEnterAsync()
	{
		_logger.LogInformation("Entering MainAppState");

		// Get the top-level container
		_top = Application.Top;

		// Create the main window
		_mainWindow = new Window("llmchat - LLM Chat Manager")
		{
			X = 0,
			Y = 1, // Leave one row for the menu
			Width = Dim.Fill(),
			Height = Dim.Fill()
		};
		_top.Add(_mainWindow);

		// Create UI elements
		CreateMenuBar();
		CreateChatInterface();
		CreateStatusBar();

		// Add initial system message
		_chatHistory.AddSystemMessage("Welcome to llmchat! This is a test environment. Your messages will receive automated test responses.");
		RefreshChatDisplay();

		await Task.CompletedTask;
	}

	public override async Task OnLeaveAsync()
	{
		_logger.LogInformation("Leaving MainAppState");

		// Clean up UI elements if needed
		_top.RemoveAll();

		await Task.CompletedTask;
	}

	public override async Task OnUnloadAsync()
	{
		_logger.LogInformation("Unloading MainAppState");

		// Shutdown Terminal.Gui
		Application.Shutdown();

		await Task.CompletedTask;
	}

	public override async Task OnUpdateAsync()
	{
		// This method is called repeatedly by the engine
		// You can use it for any periodic updates if needed
		await Task.CompletedTask;
	}

	private void CreateMenuBar()
	{
		_menuBar = new MenuBar(
		[
			new MenuBarItem("_File",
			[
				new MenuItem("_New Chat", "Ctrl+N", () => NewChat()),
				new MenuItem("_Open Chat", "Ctrl+O", () => OpenChat()),
				new MenuItem("_Save Chat", "Ctrl+S", () => SaveChat()),
				null, // Separator
                new MenuItem("_Settings", "", () => OpenSettings()),
				null, // Separator
                new MenuItem("_Quit", "Ctrl+Q", () => Quit())
			]),
			new MenuBarItem("_Edit",
			[
				new MenuItem("_Copy", "Ctrl+C", () => Copy()),
				new MenuItem("_Paste", "Ctrl+V", () => Paste()),
				new MenuItem("_Clear Chat", "", () => ClearChat())
			]),
			new MenuBarItem("_View",
			[
				new MenuItem("_Toggle Sidebar", "F3", () => ToggleSidebar()),
				new MenuItem("_Full Screen", "F11", () => ToggleFullScreen())
			]),
			new MenuBarItem("_Help",
			[
				new MenuItem("_Documentation", "F1", () => ShowDocumentation()),
				new MenuItem("_About", "", () => ShowAbout())
			])
		]);
		_top.Add(_menuBar);
	}

	private void CreateChatInterface()
	{
		// Create the chat history area (upper part)
		var chatHistoryFrame = new FrameView("Chat History")
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Fill() - 6 // Leave space for input area
		};
		_mainWindow.Add(chatHistoryFrame);

		// Create scrollable view for chat history
		_chatScrollView = new ScrollView()
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Fill(),
			ShowVerticalScrollIndicator = true,
			ShowHorizontalScrollIndicator = false
		};
		chatHistoryFrame.Add(_chatScrollView);

		// Create the container for chat messages
		_chatHistoryView = new View()
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Sized(0) // Will grow as messages are added
		};
		_chatScrollView.Add(_chatHistoryView);

		// Create the message input area (lower part)
		var inputFrame = new FrameView("Message Input")
		{
			X = 0,
			Y = Pos.Bottom(chatHistoryFrame),
			Width = Dim.Fill(),
			Height = 6
		};
		_mainWindow.Add(inputFrame);

		// Create the text input
		_messageInput = new TextView()
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill() - 10, // Leave space for send button
			Height = Dim.Fill()
		};

		// Handle Enter key to send message (Ctrl+Enter for new line)
		_messageInput.KeyPress += (e) =>
		{
			if (e.KeyEvent.Key == Key.Enter && !e.KeyEvent.IsCtrl)
			{
				e.Handled = true;
				SendMessage();
			}
		};

		inputFrame.Add(_messageInput);

		// Create the send button with emoji
		_sendButton = new Button("📤 Send")
		{
			X = Pos.Right(_messageInput) + 1,
			Y = Pos.Center(),
			Width = 8,
			Height = 1
		};
		_sendButton.Clicked += () => SendMessage();
		inputFrame.Add(_sendButton);

		// Focus on the message input
		_messageInput.SetFocus();
	}

	private void CreateStatusBar()
	{
		_statusBar = new StatusBar(
		[
			new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			new StatusItem(Key.CtrlMask | Key.N, "~^N~ New Chat", () => NewChat()),
			new StatusItem(Key.CtrlMask | Key.S, "~^S~ Save", () => SaveChat()),
			new StatusItem(Key.Enter, "~Enter~ Send", () => SendMessage()),
			new StatusItem(Key.CtrlMask | Key.Enter, "~^Enter~ New Line", () => {}),
			new StatusItem(Key.F1, "~F1~ Help", () => ShowDocumentation())
		]);
		_top.Add(_statusBar);
	}

	private void SendMessage()
	{
		var message = _messageInput.Text.ToString()?.Trim();
		if (string.IsNullOrEmpty(message))
		{
			return;
		}

		_logger.LogInformation("Sending message: {Message}", message);

		// Add user message to chat history
		_chatHistory.AddUserMessage(message);
		_logger.LogInformation("Chat history count after user message: {Count}", _chatHistory.Count);

		// Clear the input
		_messageInput.Text = "";

		// Simulate AI response (for testing)
		_messageCounter++;
		var aiResponse = GenerateTestResponse(message, _messageCounter);
		_chatHistory.AddAssistantMessage(aiResponse);
		_logger.LogInformation("Chat history count after AI response: {Count}", _chatHistory.Count);

		// Refresh the chat display
		RefreshChatDisplay();

		// Scroll to bottom
		ScrollToBottom();

		// Return focus to input
		_messageInput.SetFocus();
	}

	private string GenerateTestResponse(string userMessage, int messageNumber)
	{
		var responses = new[]
		{
			$"I received your message: \"{userMessage}\". This is test response #{messageNumber}.",
			$"Interesting! You said: \"{userMessage}\". I'm test response #{messageNumber}, and I'm here to help!",
			$"Thanks for sharing: \"{userMessage}\". As test response #{messageNumber}, I can confirm the chat is working properly.",
			$"I understand you're saying: \"{userMessage}\". This automated response (#{messageNumber}) confirms the system is functioning.",
			$"Your message \"{userMessage}\" has been received. Test reply #{messageNumber} generated successfully."
		};

		return responses[messageNumber % responses.Length];
	}

	private void RefreshChatDisplay()
	{
		// Clear existing messages
		_chatHistoryView.RemoveAll();

		int yPos = 0;
		// int maxWidth = Math.Max(20, _chatScrollView.Bounds.Width - 2); // Ensure minimum width
		int maxWidth = Math.Max(20, _mainWindow.Bounds.Width - 6);

		foreach (var message in _chatHistory)
		{
			// Create a frame for each message
			var roleText = message.Role.ToString();
			var roleColor = GetColorForRole(message.Role);

			// Create role label
			var roleLabel = new Label($"[{roleText}]")
			{
				X = 0,
				Y = yPos,
				ColorScheme = new ColorScheme()
				{
					Normal = new Terminal.Gui.Attribute(roleColor, Color.Black),
				},
			};
			_chatHistoryView.Add(roleLabel);

			// Create timestamp (if your ChatMessage has a timestamp property)
			var timestamp = DateTime.Now.ToString("HH:mm:ss");
			var timestampLabel = new Label(timestamp)
			{
				X = Pos.Right(roleLabel) + 1,
				Y = yPos,
				ColorScheme = new ColorScheme()
				{
					Normal = new Terminal.Gui.Attribute(Color.Gray, Color.Black),
				},
			};
			_chatHistoryView.Add(timestampLabel);

			yPos++;

			// Create message content with word wrapping
			var content = message.Content ?? "";
			if (string.IsNullOrEmpty(content))
			{
				content = "[Empty message]"; // Debug helper
			}

			var wrappedLines = WordWrap(content, maxWidth - 4);

			foreach (var line in wrappedLines)
			{
				var contentLabel = new Label(line)
				{
					X = 2,
					Y = yPos,
					Width = Dim.Fill()
				};
				_chatHistoryView.Add(contentLabel);
				yPos++;
			}

			// Add spacing between messages
			yPos++;
		}

		// Update the height of the chat history view
		_chatHistoryView.Height = Dim.Sized(yPos);
		_chatHistoryView.Width = Dim.Fill();

		_chatScrollView.ContentSize = new Size(
			_chatScrollView.Bounds.Width,
			yPos
		);

		// Force a redraw
		_chatScrollView.SetNeedsDisplay();
		_chatHistoryView.SetNeedsDisplay();
		// Application.Refresh(); // Add this to force immediate refresh
	}

	private List<string> WordWrap(string text, int maxWidth)
	{
		if (string.IsNullOrWhiteSpace(text))
			return new List<string> { text ?? "" };

		if (maxWidth <= 0)
			return new List<string> { text };

		var lines = new List<string>();
		var words = text.Split(' ');
		var currentLine = "";

		foreach (var word in words)
		{
			if (currentLine.Length > 0 && currentLine.Length + word.Length + 1 > maxWidth)
			{
				lines.Add(currentLine.Trim());
				currentLine = "";
			}

			currentLine += (string.IsNullOrEmpty(currentLine) ? "" : " ") + word;
		}

		if (!string.IsNullOrEmpty(currentLine))
		{
			lines.Add(currentLine.Trim());
		}

		return lines.Any() ? lines : new List<string> { "" };
	}

	private Color GetColorForRole(AuthorRole role)
	{
		if (role.Equals(AuthorRole.System)) return Color.BrightYellow;
		if (role.Equals(AuthorRole.User)) return Color.Cyan;
		if (role.Equals(AuthorRole.Assistant)) return Color.Green;
		if (role.Equals(AuthorRole.Tool)) return Color.Magenta;
		return Color.White;
	}

	private void ScrollToBottom()
	{
		if (_chatHistoryView.Bounds.Height > _chatScrollView.Bounds.Height)
		{
			_chatScrollView.ContentOffset = new Point(0, _chatHistoryView.Bounds.Height - _chatScrollView.Bounds.Height);
		}
	}

	#region Menu Actions

	private void NewChat()
	{
		_logger.LogInformation("New chat requested");
		var result = MessageBox.Query("New Chat", "Start a new chat? Current chat will be cleared.", "Yes", "No");
		if (result == 0)
		{
			_chatHistory.Clear();
			_chatHistory.AddSystemMessage("Welcome to llmchat! This is a test environment. Your messages will receive automated test responses.");
			_messageCounter = 0;
			RefreshChatDisplay();
			_messageInput.SetFocus();
		}
	}

	private void OpenChat()
	{
		_logger.LogInformation("Open chat requested");
		var dialog = new OpenDialog("Open Chat", "Select a chat file to open")
		{
			AllowsMultipleSelection = false,
			CanChooseDirectories = false,
			CanChooseFiles = true
		};

		Application.Run(dialog);

		if (!dialog.Canceled && dialog.FilePaths.Count > 0)
		{
			var filePath = dialog.FilePaths[0];
			_logger.LogInformation("Opening chat file: {FilePath}", filePath);
			MessageBox.Query("Open Chat", $"Would open: {filePath}", "Ok");
		}
	}

	private void SaveChat()
	{
		_logger.LogInformation("Save chat requested");
		var dialog = new SaveDialog("Save Chat", "Save current chat");

		Application.Run(dialog);

		if (!dialog.Canceled)
		{
			var filePath = dialog.FilePath?.ToString();
			_logger.LogInformation("Saving chat to: {FilePath}", filePath);
			MessageBox.Query("Save Chat", $"Would save to: {filePath}", "Ok");
		}
	}

	private void OpenSettings()
	{
		_logger.LogInformation("Settings requested");
		MessageBox.Query("Settings", "Settings dialog coming soon!", "Ok");
	}

	private void Quit()
	{
		_logger.LogInformation("Quit requested");
		var result = MessageBox.Query("Quit", "Are you sure you want to quit?", "Yes", "No");
		if (result == 0)
		{
			_engine.RequestStop();
		}
	}

	private void Copy()
	{
		_logger.LogInformation("Copy requested");
		// TODO: Implement copy functionality
	}

	private void Paste()
	{
		_logger.LogInformation("Paste requested");
		// TODO: Implement paste functionality
	}

	private void ClearChat()
	{
		_logger.LogInformation("Clear chat requested");
		var result = MessageBox.Query("Clear Chat", "Clear the current chat history?", "Yes", "No");
		if (result == 0)
		{
			_chatHistory.Clear();
			_chatHistory.AddSystemMessage("Chat cleared. Ready for new conversation.");
			_messageCounter = 0;
			RefreshChatDisplay();
		}
	}

	private void ToggleSidebar()
	{
		_logger.LogInformation("Toggle sidebar requested");
		MessageBox.Query("Sidebar", "Sidebar toggle coming soon!", "Ok");
	}

	private void ToggleFullScreen()
	{
		_logger.LogInformation("Toggle fullscreen requested");
		// TODO: Implement fullscreen toggle
	}

	private void ShowDocumentation()
	{
		_logger.LogInformation("Documentation requested");
		MessageBox.Query("Documentation",
			"llmchat - Terminal-based LLM Chat Manager\n\n" +
			"Keyboard Shortcuts:\n" +
			"Ctrl+N - New Chat\n" +
			"Ctrl+O - Open Chat\n" +
			"Ctrl+S - Save Chat\n" +
			"Ctrl+Q - Quit\n" +
			"Enter - Send Message\n" +
			"Ctrl+Enter - New Line in Message\n" +
			"F1 - Help\n" +
			"F3 - Toggle Sidebar",
			"Ok");
	}

	private void ShowAbout()
	{
		_logger.LogInformation("About dialog requested");
		MessageBox.Query("About",
			"llmchat v0.1.0\n" +
			"A Terminal-based LLM Chat Manager\n\n" +
			"Built with Terminal.Gui\n" +
			"Using Microsoft Semantic Kernel",
			"Ok");
	}

	#endregion

	#endregion
}