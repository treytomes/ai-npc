using LLM.Intent.Classification.Facts;
using NRules;

namespace LLM.Intent.Classification;

interface ISessionInitializer<TActor>
{
	void Initialize(ISession session, string utterance, TActor actor, RecentIntent? recentIntent);
}
