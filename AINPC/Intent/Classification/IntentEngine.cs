using AINPC.Entities;
using AINPC.Intent.Classification.Facts;

namespace AINPC.Intent.Classification;

internal sealed class IntentEngine
	: IIntentEngine<Actor>
{
	#region Fields

	private readonly IIntentClassifier _classifier = new IntentClassifier();

	#endregion

	#region Methods

	public async Task<IntentEngineResult> ProcessAsync(
		string input,
		Actor actor,
		IntentEngineContext? context = null)
	{
		var result = await _classifier.Classify(
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
