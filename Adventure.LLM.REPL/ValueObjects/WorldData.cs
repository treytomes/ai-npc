namespace Adventure.LLM.REPL.ValueObjects;

public record WorldData
{
	public Room Room { get; set; } = new();
}
