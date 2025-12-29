using YamlDotNet.Serialization;

namespace Adventure.LLM.REPL.Templating;

public class ExecutionSettings
{
	public float Temperature { get; set; } = 0.7f;
	public int MaxTokens { get; set; } = 150;

	[YamlMember(Alias = "stop_sequences")]
	public List<string>? StopSequences { get; set; }
}
