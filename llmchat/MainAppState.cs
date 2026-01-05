using Adventure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Terminal.Gui;

namespace llmchat;

internal sealed class MainAppState : AppState
{
	private readonly ILogger<MainAppState> _logger;
	private readonly AppSettings _settings;
	private readonly TerminalGuiAppEngine _engine;
	private readonly ChatHistory _chatHistory;

	private Toplevel _top = null!;
	private Window _mainWindow = null!;
	private MenuBar _menuBar = null!;
	private StatusBar _statusBar = null!;

	private ChatHistoryView _chatView = null!;
	private TextView _messageInput = null!;
	private Button _sendButton = null!;

	private int _messageCounter = 0;

	public MainAppState(
		IOptions<AppSettings> settings,
		IStateManager states,
		ILogger<MainAppState> logger,
		IAppEngine engine
	) : base(states)
	{
		_settings = settings.Value;
		_logger = logger;
		_engine = engine as TerminalGuiAppEngine
			?? throw new InvalidOperationException("MainAppState requires TerminalGuiAppEngine");

		_chatHistory = new ChatHistory();
	}

	// ---------------- Lifecycle ----------------

	public override async Task OnLoadAsync()
	{
		_logger.LogInformation("Loading MainAppState");
		Application.Init();
		await Task.CompletedTask;
	}

	public override async Task OnEnterAsync()
	{
		_logger.LogInformation("Entering MainAppState");

		_top = Application.Top;

		CreateMenuBar();
		CreateMainWindow();
		CreateStatusBar();

		_chatHistory.AddSystemMessage(
			"Welcome to llmchat! This is a test environment. Your messages will receive automated test responses."
		);

		RefreshChatDisplay();
		_messageInput.SetFocus();

		await Task.CompletedTask;
	}

	public override async Task OnLeaveAsync()
	{
		_logger.LogInformation("Leaving MainAppState");
		_top.RemoveAll();
		await Task.CompletedTask;
	}

	public override async Task OnUnloadAsync()
	{
		_logger.LogInformation("Unloading MainAppState");
		Application.Shutdown();
		await Task.CompletedTask;
	}

	public override async Task OnUpdateAsync()
	{
		await Task.CompletedTask;
	}

	// ---------------- UI Construction ----------------

	private void CreateMainWindow()
	{
		_mainWindow = new Window("llmchat – LLM Chat Manager")
		{
			X = 0,
			Y = 1,
			Width = Dim.Fill(),
			Height = Dim.Fill() - 1
		};

		_top.Add(_mainWindow);

		_chatView = new ChatHistoryView
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Fill() - 6
		};

		_mainWindow.Add(_chatView);

		var inputFrame = new FrameView("Message Input")
		{
			X = 0,
			Y = Pos.Bottom(_chatView),
			Width = Dim.Fill(),
			Height = 6
		};

		_mainWindow.Add(inputFrame);

		_messageInput = new TextView
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill() - 12,
			Height = Dim.Fill(),
			WordWrap = true
		};

		_messageInput.KeyPress += e =>
		{
			if (e.KeyEvent.Key == Key.Enter && !e.KeyEvent.IsCtrl)
			{
				e.Handled = true;
				SendMessage();
			}
		};

		inputFrame.Add(_messageInput);

		_sendButton = new Button("Send")
		{
			X = Pos.Right(_messageInput) + 1,
			Y = Pos.Center(),
			Width = 10,
			Height = 1
		};

		_sendButton.Clicked += SendMessage;
		inputFrame.Add(_sendButton);
	}

	private void CreateMenuBar()
	{
		_menuBar = new MenuBar(new[]
		{
			new MenuBarItem("_File", new[]
			{
				new MenuItem("_New Chat", "Ctrl+N", NewChat),
				new MenuItem("_Save Chat", "Ctrl+S", SaveChat),
				null,
				new MenuItem("_Quit", "Ctrl+Q", Quit)
			}),
			new MenuBarItem("_Help", new[]
			{
				new MenuItem("_About", "", ShowAbout)
			})
		});

		_top.Add(_menuBar);
	}

	private void CreateStatusBar()
	{
		_statusBar = new StatusBar(new[]
		{
			new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", Quit),
			new StatusItem(Key.CtrlMask | Key.N, "~^N~ New Chat", NewChat),
			new StatusItem(Key.Enter, "~Enter~ Send", SendMessage)
		});

		_top.Add(_statusBar);
	}

	// ---------------- Chat Logic ----------------

	private void SendMessage()
	{
		var text = _messageInput.Text.ToString()?.Trim();
		if (string.IsNullOrEmpty(text)) return;

		_chatHistory.AddUserMessage(text);
		_messageInput.Text = string.Empty;

		_messageCounter++;
		_chatHistory.AddAssistantMessage(GenerateTestResponse(text, _messageCounter));

		RefreshChatDisplay();
		_messageInput.SetFocus();
	}

	private void RefreshChatDisplay()
	{
		_chatView.Render(_chatHistory);
	}

	private static string GenerateTestResponse(string input, int n)
		=> $"Echo #{n}: {input}";

	// ---------------- Menu Actions ----------------

	private void NewChat()
	{
		_chatHistory.Clear();
		_chatHistory.AddSystemMessage("New chat started.");
		_messageCounter = 0;
		RefreshChatDisplay();
	}

	private void SaveChat()
	{
		MessageBox.Query("Save", "Save not implemented yet.", "Ok");
	}

	private void Quit()
	{
		if (MessageBox.Query("Quit", "Exit llmchat?", "Yes", "No") == 0)
			_engine.RequestStop();
	}

	private void ShowAbout()
	{
		MessageBox.Query(
			"About",
			"llmchat\nTerminal-based LLM Chat Manager\nBuilt with Terminal.Gui v2",
			"Ok"
		);
	}
}
