using NRules;

namespace LLM.Intent.Classification;

internal interface IIntentAggregator
{
	IReadOnlyList<Facts.Intent> Aggregate(ISession session);
}
