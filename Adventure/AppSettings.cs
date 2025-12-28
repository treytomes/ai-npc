namespace Adventure;

class AppSettings
{
	public bool Debug { get; set; } = false;
	public string OllamaUrl { get; set; } = "http://localhost:11434";
	public string ModelId { get; set; } = "qwen2.5:0.5b";
}