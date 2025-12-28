namespace LLM.NLP;

/// <summary>
/// Define a process that mutates an intent seed.
/// </summary>
public interface IIntentPipelineStep
{
	IntentSeed Process(IntentSeed seed);
}