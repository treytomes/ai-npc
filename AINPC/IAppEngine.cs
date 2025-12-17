namespace AINPC;

/// <summary>
/// Load up required global resources and launch the initial state.
/// </summary>
interface IAppEngine : IStateManager
{
	Task RunAsync<TAppState>() where TAppState : AppState;
}
