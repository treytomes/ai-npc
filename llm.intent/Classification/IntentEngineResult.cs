using LLM.Intent.Classification.Facts;

namespace LLM.Intent.Classification;

internal sealed class IntentEngineResult
{
	public IReadOnlyList<Intent> Intents { get; init; } = [];
	public IReadOnlyList<string> FiredRules { get; init; } = [];
	public RecentIntent? UpdatedRecentIntent { get; init; }
}
