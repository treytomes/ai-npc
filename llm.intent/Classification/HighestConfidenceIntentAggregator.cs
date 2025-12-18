using LLM.Intent.Classification.Facts;
using NRules;

namespace LLM.Intent.Classification;

internal sealed class HighestConfidenceIntentAggregator : IIntentAggregator
{
	public IReadOnlyList<Facts.Intent> Aggregate(ISession session)
	{
		var suppressed = session.Query<SuppressedIntent>()
			.Select(s => s.Name)
			.ToHashSet();

		return session.Query<Facts.Intent>()
			.Where(i => !suppressed.Contains(i.Name))
			.GroupBy(i => i, new IntentEqualityComparer())
			.Select(g => g.MaxBy(i => i.Confidence)!)
			.OrderByDescending(i => i.Confidence)
			.ToList();
	}
}
