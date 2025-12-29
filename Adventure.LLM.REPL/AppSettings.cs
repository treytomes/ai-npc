namespace Adventure.LLM.REPL;

internal sealed class AppSettings : Adventure.AppSettings
{
	public string OllamaUrl { get; set; } = "http://localhost:11434";
	public string ModelId { get; set; } = "qwen2.5:0.5b";
}