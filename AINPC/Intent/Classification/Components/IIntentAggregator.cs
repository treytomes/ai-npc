using NRules;

namespace AINPC.Intent.Classification;

internal interface IIntentAggregator
{
	IReadOnlyList<Facts.Intent> Aggregate(ISession session);
}
