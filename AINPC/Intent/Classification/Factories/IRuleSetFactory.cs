using NRules.RuleModel;

namespace AINPC.Intent.Classification.Factories;

internal interface IRuleSetFactory
{
	IRuleSet GetRules(string actorRole);
}