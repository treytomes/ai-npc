namespace AINPC;

abstract class AppState
{
	#region Fields

	private readonly IStateManager _states;
	private bool _disposedValue;

	#endregion

	#region Constructors

	public AppState(IStateManager states)
	{
		_states = states ?? throw new ArgumentNullException(nameof(states));
	}

	#endregion

	#region Methods

	public abstract Task LoadAsync();
	public abstract Task UnloadAsync();
	public abstract Task UpdateAsync();

	protected async Task LeaveAsync()
	{
		await _states.LeaveStateAsync();
	}

	#endregion
}