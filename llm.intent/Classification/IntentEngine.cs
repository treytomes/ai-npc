using LLM.Intent.Classification.Facts;
using LLM.Intent.Entities;

namespace LLM.Intent.Classification;

internal sealed class IntentEngine
	: IIntentEngine<Actor>
{
	#region Fields

	private readonly IIntentClassifier _classifier = new IntentClassifier();

	#endregion

	#region Methods

	public IntentEngineResult Process(
		string input,
		Actor actor,
		IntentEngineContext? context = null)
	{
		var result = _classifier.Classify(
			input,
			actor,
			context?.RecentIntent);

		var strongest = result.Intents
			.OrderByDescending(i => i.Confidence)
			.FirstOrDefault();

		return new()
		{
			Intents = result.Intents,
			FiredRules = result.FiredRules,
			UpdatedRecentIntent = strongest != null
				? new RecentIntent(strongest.Name, strongest.Confidence)
				: context?.RecentIntent?.Decay()
		};
	}

	#endregion
}
