using Catalyst;

namespace LLM.NLP.Services;

/// <summary>
/// Converts processed NLP documents into normalized, game-friendly
/// representations that can be consumed by application logic or LLMs.
/// </summary>
public interface INlpParser
{
	/// <summary>
	/// Parses a processed NLP document into a structured input model.
	/// </summary>
	/// <param name="document">
	/// A document that has already been processed by the NLP runtime.
	/// </param>
	/// <returns>A normalized <see cref="ParsedInput"/> instance.</returns>
	ParsedInput Parse(Document document);
}
