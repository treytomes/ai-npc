namespace Adventure.LLM.REPL.ValueObjects;

public record SpatialSummary
{
	public string Shape { get; set; } = string.Empty;
	public string Size { get; set; } = string.Empty;
	public string Lighting { get; set; } = string.Empty;
	public List<string> Smell { get; set; } = new();
}
