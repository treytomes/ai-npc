namespace Adventure;

class AppSettingsV0 : AppSettings
{
	public string OllamaUrl { get; set; } = "http://localhost:11434";
	public string ModelId { get; set; } = "qwen2.5:0.5b";
}