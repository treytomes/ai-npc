namespace Adventure.LLM.REPL.ValueObjects;

public record FeatureFacts
{
	public string Material { get; set; } = string.Empty;
	public string Condition { get; set; } = string.Empty;
	public List<string> Details { get; set; } = new();
}
