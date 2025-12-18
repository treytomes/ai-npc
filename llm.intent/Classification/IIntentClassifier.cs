using LLM.Intent.Classification.Facts;
using LLM.Intent.Entities;

namespace LLM.Intent.Classification;

internal interface IIntentClassifier
{
	IntentClassificationResult Classify(string utterance, Actor actor, RecentIntent? recentIntent = null);
}
