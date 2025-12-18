using AINPC.Intent.Classification.Facts;
using NRules;

namespace AINPC.Intent.Classification;

interface ISessionInitializer<TActor>
{
	void Initialize(ISession session, string utterance, TActor actor, RecentIntent? recentIntent);
}
