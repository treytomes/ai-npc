using AINPC.Entities;
using AINPC.Intent.Classification.Facts;

namespace AINPC.Intent.Classification;

internal interface IIntentClassifier
{
	IntentClassificationResult Classify(string utterance, Actor actor, RecentIntent? recentIntent = null);
}
