using Adventure;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Terminal.Gui;

namespace llmchat;

internal sealed class MainAppState : AppState
{
	#region Fields

	private readonly ILogger<MainAppState> _logger;
	private readonly AppSettings _settings;
	private readonly TerminalGuiAppEngine _engine;

	private Toplevel _top;
	private Window _mainWindow;
	private MenuBar _menuBar;
	private StatusBar _statusBar;

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
		CreateMainContent();
		CreateStatusBar();

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
		_menuBar = new MenuBar(new MenuBarItem[]
		{
			new MenuBarItem("_File", new MenuItem[]
			{
				new MenuItem("_New Chat", "Ctrl+N", () => NewChat()),
				new MenuItem("_Open Chat", "Ctrl+O", () => OpenChat()),
				new MenuItem("_Save Chat", "Ctrl+S", () => SaveChat()),
				null, // Separator
                new MenuItem("_Settings", "", () => OpenSettings()),
				null, // Separator
                new MenuItem("_Quit", "Ctrl+Q", () => Quit())
			}),
			new MenuBarItem("_Edit", new MenuItem[]
			{
				new MenuItem("_Copy", "Ctrl+C", () => Copy()),
				new MenuItem("_Paste", "Ctrl+V", () => Paste()),
				new MenuItem("_Clear", "", () => Clear())
			}),
			new MenuBarItem("_View", new MenuItem[]
			{
				new MenuItem("_Toggle Sidebar", "F3", () => ToggleSidebar()),
				new MenuItem("_Full Screen", "F11", () => ToggleFullScreen())
			}),
			new MenuBarItem("_Help", new MenuItem[]
			{
				new MenuItem("_Documentation", "F1", () => ShowDocumentation()),
				new MenuItem("_About", "", () => ShowAbout())
			})
		});
		_top.Add(_menuBar);
	}

	private void CreateMainContent()
	{
		// Create a welcome label for now
		var welcomeLabel = new Label("Welcome to llmchat!")
		{
			X = Pos.Center(),
			Y = Pos.Center() - 2
		};
		_mainWindow.Add(welcomeLabel);

		var instructionLabel = new Label("Terminal.Gui is working correctly. Press Ctrl+Q to quit.")
		{
			X = Pos.Center(),
			Y = Pos.Center()
		};
		_mainWindow.Add(instructionLabel);

		// Add a button to test interaction
		var newChatButton = new Button("_New Chat")
		{
			X = Pos.Center(),
			Y = Pos.Center() + 2
		};
		newChatButton.Clicked += () => NewChat();
		_mainWindow.Add(newChatButton);
	}

	private void CreateStatusBar()
	{
		_statusBar = new StatusBar(new StatusItem[]
		{
						new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Quit()),
			new StatusItem(Key.CtrlMask | Key.N, "~^N~ New Chat", () => NewChat()),
			new StatusItem(Key.CtrlMask | Key.S, "~^S~ Save", () => SaveChat()),
			new StatusItem(Key.F1, "~F1~ Help", () => ShowDocumentation()),
			new StatusItem(Key.F3, "~F3~ Sidebar", () => ToggleSidebar())
		});
		_top.Add(_statusBar);
	}

	#region Menu Actions

	private void NewChat()
	{
		_logger.LogInformation("New chat requested");
		MessageBox.Query("New Chat", "New chat functionality coming soon!", "Ok");
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
		// TODO: Transition to settings state
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

	private void Clear()
	{
		_logger.LogInformation("Clear requested");
		var result = MessageBox.Query("Clear", "Clear the current chat?", "Yes", "No");
		if (result == 0)
		{
			// TODO: Clear chat content
		}
	}

	private void ToggleSidebar()
	{
		_logger.LogInformation("Toggle sidebar requested");
		// TODO: Implement sidebar toggle
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
			"Built with Terminal.Gui",
			"Ok");
	}

	#endregion

	#endregion
}