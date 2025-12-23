using NRules;

namespace AINPC.Intent.Classification;

interface IEvidenceProvider<TActor>
{
	Task ProvideAsync(ISession session, string utterance, TActor actor);
}
