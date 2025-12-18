using AINPC.Intent.Classification.Facts;

namespace AINPC.Intent.Classification;

internal sealed class IntentEngineContext
{
	public RecentIntent? RecentIntent { get; init; }
}
