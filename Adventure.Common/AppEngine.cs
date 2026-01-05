using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace Adventure;

/// <inheritdoc/>
public class AppEngine : IAppEngine
{
	#region Fields

	private IServiceProvider _serviceProvider;
	private readonly ILogger<AppEngine> _logger;
	private readonly Stack<AppState> _states = new();

	#endregion

	#region Constructors

	public AppEngine(IServiceProvider serviceProvider, ILogger<AppEngine> logger)
	{
		_serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	#endregion

	#region Properties

	protected bool HasState => _states.Any();
	protected AppState CurrentState => _states.Peek();

	#endregion

	#region Methods

	public virtual async Task RunAsync<TAppState>()
		where TAppState : AppState
	{
		try
		{
			await InitializeAsync();

			await EnterStateAsync<TAppState>();

			while (HasState)
			{
				await CurrentState.OnUpdateAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in RunAsync.");
			AnsiConsole.WriteException(ex);
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

	/// <summary>
	/// Enter a new state, but leave the old one loaded.
	/// </summary>
	/// <typeparam name="TAppState"></typeparam>
	/// <returns></returns>
	public async Task EnterStateAsync<TAppState>()
		where TAppState : AppState
	{
		try
		{
			if (_states.TryPeek(out var oldState))
			{
				await oldState.OnLeaveAsync();
			}
			var newState = _serviceProvider.GetRequiredService<TAppState>();
			await newState.OnLoadAsync();
			await newState.OnEnterAsync();
			_states.Push(newState);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to enter state: {AppState}", typeof(TAppState).Name);
			AnsiConsole.WriteException(ex);
		}
	}

	/// <summary>
	/// Leave and unload the current state, returning to the previous state.
	/// </summary>
	/// <returns></returns>
	public async Task LeaveStateAsync()
	{
		try
		{
			if (_states.TryPop(out var oldState))
			{
				await oldState.OnLeaveAsync();
				await oldState.OnUnloadAsync();
			}
			if (_states.TryPeek(out var newState))
			{
				await newState.OnEnterAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to leave current state.");
			AnsiConsole.WriteException(ex);
		}
	}

	public async Task UpdateAsync()
	{
		try
		{
			if (_states.TryPeek(out var state))
			{
				await state.OnUpdateAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to update current state.");
			AnsiConsole.WriteException(ex);
		}
	}

	protected virtual async Task InitializeAsync()
	{
		await Task.CompletedTask;
	}

	protected virtual async Task DestroyAsync()
	{
		await Task.CompletedTask;
	}

	#endregion
}
