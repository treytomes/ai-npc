namespace LLM.NLP.Services;

public interface IIntentSeedExtractor
{
	IntentSeed Extract(ParsedInput input);
}