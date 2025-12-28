using Adventure.Entities;
using Adventure.Intent.Classification.Facts;

namespace Adventure.Intent.Classification;

internal interface IIntentClassifier
{
	Task<IntentClassificationResult> Classify(string utterance, Actor actor, RecentIntent? recentIntent = null);
}
