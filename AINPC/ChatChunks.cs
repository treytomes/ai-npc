using AINPC.ValueObjects;

namespace AINPC;

// Base type for all chunks.
internal abstract record ChatChunk;

// Specific chunk types.
internal record RuleChunk(IReadOnlyList<string> FiredRules) : ChatChunk;
internal record IntentChunk(IReadOnlyList<Intent.Classification.Facts.Intent> Intents) : ChatChunk;
internal record TextChunk(string Text) : ChatChunk;
internal record ItemResolutionChunk(ItemResolutionResult Result) : ChatChunk;
internal record ToolResultChunk(string ToolName, object Result) : ChatChunk;