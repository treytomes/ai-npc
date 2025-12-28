using Adventure.Entities;
using Adventure.Intent.Classification.Facts;
using Adventure.Intent.Facts;
using NRules;

namespace Adventure.Intent.Classification;

internal sealed class SessionInitializer
	: ISessionInitializer<Actor>
{
	public void Initialize(
		ISession session,
		string utterance,
		Actor actor,
		RecentIntent? recentIntent)
	{
		session.Insert(new UserUtterance(utterance));
		session.Insert(new ActorRole(actor.Role.Name));

		if (recentIntent != null)
		{
			session.Insert(recentIntent);
		}
	}
}
