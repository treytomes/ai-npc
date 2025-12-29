namespace Adventure.LLM.REPL.Configuration;

public class AppConfiguration
{
	public RenderingConfig Rendering { get; set; } = new();
	public ValidationConfig Validation { get; set; } = new();
}
