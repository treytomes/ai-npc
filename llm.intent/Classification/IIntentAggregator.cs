using NRules;

namespace LLM.Intent.Classification;

internal interface IIntentAggregator
{
	IReadOnlyList<Intent> Aggregate(ISession session);
}
