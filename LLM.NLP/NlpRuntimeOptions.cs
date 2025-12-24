using Mosaik.Core;

namespace LLM.NLP;

public sealed class NlpRuntimeOptions
{
	public string DataPath { get; set; } = "catalyst-data";
	public Language Language { get; set; } = Language.English;
}
