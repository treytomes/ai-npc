namespace Adventure;

/// <summary>
/// Load up required global resources and launch the initial state.
/// </summary>
public interface IAppEngine : IStateManager
{
	Task RunAsync<TAppState>() where TAppState : AppState;
}
