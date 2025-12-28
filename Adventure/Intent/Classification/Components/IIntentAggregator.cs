using NRules;

namespace Adventure.Intent.Classification;

internal interface IIntentAggregator
{
	IReadOnlyList<Facts.Intent> Aggregate(ISession session);
}
