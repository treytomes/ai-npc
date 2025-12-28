using Adventure.Intent.Classification.Facts;
using NRules;

namespace Adventure.Intent.Classification;

interface ISessionInitializer<TActor>
{
	void Initialize(ISession session, string utterance, TActor actor, RecentIntent? recentIntent);
}
