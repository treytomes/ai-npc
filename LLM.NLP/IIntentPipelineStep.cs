namespace LLM.NLP;

public interface IIntentPipelineStep
{
	IntentSeed Process(IntentSeed seed);
}