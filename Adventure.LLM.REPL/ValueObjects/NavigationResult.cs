namespace Adventure.LLM.REPL.ValueObjects;

public record NavigationResult
{
	public bool Success { get; init; }
	public string? Message { get; init; }
	public string? NewRoomKey { get; init; }

	public static NavigationResult Succeeded(string roomKey) => new()
	{
		Success = true,
		NewRoomKey = roomKey
	};

	public static NavigationResult Failed(string message) => new()
	{
		Success = false,
		Message = message
	};
}
