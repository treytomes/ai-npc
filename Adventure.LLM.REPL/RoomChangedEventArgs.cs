using Adventure.LLM.REPL.ValueObjects;

namespace Adventure.LLM.REPL;

public class RoomChangedEventArgs : EventArgs
{
	public string PreviousRoomKey { get; init; } = string.Empty;
	public string NewRoomKey { get; init; } = string.Empty;
	public WorldData? NewRoomData { get; init; }
}