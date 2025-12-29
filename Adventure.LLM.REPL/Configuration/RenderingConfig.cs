namespace Adventure.LLM.REPL.Configuration;

public class RenderingConfig
{
	public string SentenceCount { get; set; } = "3-5";
	public float Temperature { get; set; } = 0.15f;
	public int MaxTokens { get; set; } = 120;
}
