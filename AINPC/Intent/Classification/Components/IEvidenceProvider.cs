using NRules;

namespace AINPC.Intent.Classification;

interface IEvidenceProvider<TActor>
{
	void Provide(ISession session, string utterance, TActor actor);
}
