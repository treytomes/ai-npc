using Catalyst;

namespace LLM.NLP.Services;

/// <summary>
/// Provides access to the NLP runtime used to preprocess user input
/// before it is passed to higher-level systems such as an LLM narrator.
/// </summary>
public interface INlpRuntime
{
	/// <summary>
	/// Processes raw user input into a Catalyst document using the
	/// configured NLP pipeline.
	/// </summary>
	/// <param name="input">The raw user input text.</param>
	/// <returns>
	/// A processed <see cref="Document"/> containing tokenization,
	/// sentence segmentation, and other NLP annotations.
	/// </returns>
	Document Process(string input);
}
