using AINPC.Intent.Classification.Facts;

namespace AINPC.Intent.Classification;

internal sealed class IntentEngineResult
{
	public IReadOnlyList<Facts.Intent> Intents { get; init; } = [];
	public IReadOnlyList<string> FiredRules { get; init; } = [];
	public RecentIntent? UpdatedRecentIntent { get; init; }
}
