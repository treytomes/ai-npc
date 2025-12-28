namespace Adventure.NLP.Services;

public interface IIntentSeedExtractor
{
	IntentSeed Extract(ParsedInput input);
}