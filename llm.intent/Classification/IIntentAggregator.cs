using NRules;

namespace LLM.Intent.Classification;

interface IIntentAggregator
{
	IReadOnlyList<Intent> Aggregate(ISession session);
}
