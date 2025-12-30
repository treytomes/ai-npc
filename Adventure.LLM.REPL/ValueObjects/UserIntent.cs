namespace Adventure.LLM.REPL.ValueObjects;

public record UserIntent
{
	public string Intent { get; set; } = "look";
	public string Focus { get; set; } = string.Empty;
	public string OriginalInput { get; set; } = string.Empty;
	public bool IsValid { get; set; } = true;
	public string? Error { get; set; }
}