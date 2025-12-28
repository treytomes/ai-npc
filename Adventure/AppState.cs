namespace Adventure;

abstract class AppState
{
	#region Fields

	private readonly IStateManager _states;

	#endregion

	#region Constructors

	public AppState(IStateManager states)
	{
		_states = states ?? throw new ArgumentNullException(nameof(states));
	}

	#endregion

	#region Methods

	public abstract Task OnLoadAsync();
	public abstract Task OnUnloadAsync();
	public abstract Task OnUpdateAsync();
	public abstract Task OnEnterAsync();
	public abstract Task OnLeaveAsync();

	protected async Task LeaveAsync()
	{
		await _states.LeaveStateAsync();
	}

	protected async Task EnterStateAsync<TAppState>()
		where TAppState : AppState
	{
		await _states.EnterStateAsync<TAppState>();
	}

	#endregion
}