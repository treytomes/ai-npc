namespace LLM.NLP;

public interface IIntentSeedExtractor
{
	IntentSeed Extract(ParsedInput input);
}