using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AINPC;

/// <inheritdoc/>
class AppEngine : IAppEngine
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

	#region Methods

	public async Task RunAsync<TAppState>()
		where TAppState : AppState
	{
		try
		{
			await InitializeAsync();

			await EnterStateAsync<TAppState>();

			while (_states.Count > 0)
			{
				var currentState = _states.Peek();
				await currentState.UpdateAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error in RunAsync.");
		}
		finally
		{
			// Unload any remaining active states.
			while (_states.TryPop(out var state))
			{
				await state.UnloadAsync();
			}

			await DestroyAsync();
		}
	}

	public async Task EnterStateAsync<TAppState>()
		where TAppState : AppState
	{
		try
		{
			var newState = _serviceProvider.GetRequiredService<TAppState>();
			await newState.LoadAsync();
			_states.Push(newState);
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to enter state: {AppState}", typeof(TAppState).Name);
		}
	}

	public async Task LeaveStateAsync()
	{
		try
		{
			if (_states.TryPop(out var state))
			{
				await state.UnloadAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to leave current state.");
		}
	}

	public async Task UpdateAsync()
	{
		try
		{
			if (_states.TryPeek(out var state))
			{
				await state.UpdateAsync();
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to update current state.");
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
