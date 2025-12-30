using Adventure.LLM.REPL.ValueObjects;

namespace Adventure.LLM.REPL.Persistence;

public interface IRoomRepository
{
	/// <summary>
	/// Loads all room data from the configured source.
	/// </summary>
	Task LoadRoomsAsync(CancellationToken cancellationToken = default);

	/// <summary>
	/// Gets a room by its key.
	/// </summary>
	WorldData? GetRoom(string roomKey);

	/// <summary>
	/// Gets all available room keys.
	/// </summary>
	IEnumerable<string> GetRoomKeys();

	/// <summary>
	/// Checks if a room exists.
	/// </summary>
	bool RoomExists(string roomKey);

	/// <summary>
	/// Gets all loaded rooms.
	/// </summary>
	IReadOnlyDictionary<string, WorldData> GetAllRooms();

	/// <summary>
	/// Checks if rooms can be navigated between.
	/// </summary>
	bool CanNavigate(string fromRoom, string toRoom);

	/// <summary>
	/// Gets available exits from a room.
	/// </summary>
	IEnumerable<string> GetExits(string roomKey);

	/// <summary>
	/// Reloads all room data.
	/// </summary>
	Task ReloadRoomsAsync(CancellationToken cancellationToken = default);
}