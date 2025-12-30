using Adventure.LLM.REPL.ValueObjects;

namespace Adventure.LLM.REPL.Services;

public interface IRoomNavigationService
{
	/// <summary>
	/// Gets the current room key
	/// </summary>
	string CurrentRoomKey { get; }

	/// <summary>
	/// Gets the current room data
	/// </summary>
	WorldData? CurrentRoom { get; }

	/// <summary>
	/// Attempts to move to a different room
	/// </summary>
	Task<NavigationResult> NavigateToAsync(string roomKey);

	/// <summary>
	/// Gets available destinations from current room
	/// </summary>
	IEnumerable<string> GetAvailableDestinations();

	/// <summary>
	/// Checks if navigation to a room is possible
	/// </summary>
	bool CanNavigateTo(string roomKey);

	/// <summary>
	/// Event raised when room changes
	/// </summary>
	event EventHandler<RoomChangedEventArgs>? RoomChanged;
}
