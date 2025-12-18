using AINPC.Entities;
using AINPC.Intent.Classification.Facts;
using AINPC.Intent.Facts;
using NRules;

namespace AINPC.Intent.Classification;

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
