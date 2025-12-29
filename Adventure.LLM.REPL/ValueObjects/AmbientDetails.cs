namespace Adventure.LLM.REPL.ValueObjects;

public record AmbientDetails
{
	public List<string> Always { get; set; } = new();
}
