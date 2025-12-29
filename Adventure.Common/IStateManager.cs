namespace Adventure;

public interface IStateManager
{
	Task EnterStateAsync<TAppState>() where TAppState : AppState;
	Task LeaveStateAsync();
}
