namespace Adventure.LLM.REPL;

internal sealed class AppSettings : Adventure.AppSettings
{
	public string OllamaUrl { get; set; } = "http://localhost:11434";
	public string ModelId { get; set; } = "qwen2.5:0.5b";
	public string AssetsPath { get; set; } = "assets";
	public string RoomAssetsPath { get; set; } = "rooms";
	public string PromptAssetsPath { get; set; } = "prompts";
	public string InitialRoomName { get; set; } = "main_lab";
}