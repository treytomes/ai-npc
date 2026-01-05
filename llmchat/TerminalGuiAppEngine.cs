using Adventure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Terminal.Gui;

namespace llmchat;

/// <summary>
/// Specialized AppEngine for Terminal.Gui applications
/// </summary>
public class TerminalGuiAppEngine : AppEngine
{
	private bool _isRunning;
	private readonly ILogger<TerminalGuiAppEngine> _logger;

	public TerminalGuiAppEngine(IServiceProvider serviceProvider, ILogger<TerminalGuiAppEngine> logger)
		: base(serviceProvider, logger)
	{
		_logger = logger;
	}

	public override async Task RunAsync<TAppState>()
	{
		try
		{
			await InitializeAsync();
			await EnterStateAsync<TAppState>();

			// Run the Terminal.Gui application loop.
			_isRunning = true;

			// Start the Terminal.Gui main loop in a separate task.
			var guiTask = Task.Run(() => Application.Run());

			// Run our state update loop.
			while (_isRunning && !Application.Current.Running)
			{
				if (HasState)
				{
					await CurrentState.OnUpdateAsync();
				}

				// Small delay to prevent tight loop.
				await Task.Delay(50);
			}

			// Request stop if still running.
			if (!Application.Current.Running)
			{
				Application.RequestStop();
			}

			await guiTask;
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in RunAsync.");
			// Note: We can't use AnsiConsole with Terminal.Gui.
		}
		finally
		{
			// Unload any remaining active states
			while (HasState)
			{
				await LeaveStateAsync();
			}

			await DestroyAsync();
		}
	}

	public void RequestStop()
	{
		_isRunning = false;
		Application.RequestStop();
	}

	protected override async Task InitializeAsync()
	{
		await base.InitializeAsync();
		_logger.LogInformation("Initializing Terminal.Gui application engine");
	}

	protected override async Task DestroyAsync()
	{
		await base.DestroyAsync();
		_logger.LogInformation("Destroying Terminal.Gui application engine");
	}
}