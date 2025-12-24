using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;

namespace LLM.NLP.Test;

/// <summary>
/// Regression tests to ensure punctuation handling remains stable
/// during NLP parsing.
/// </summary>
public class NlpParser_PunctuationTests
{
	[Fact]
	public void Parser_Removes_TerminalPunctuation()
	{
		// ARRANGE
		var services = new ServiceCollection();

		services.AddNlpRuntime(o =>
		{
			o.DataPath = "catalyst-data";
			o.Language = Language.English;
		});

		using var provider = services.BuildServiceProvider();

		var runtime = provider.GetRequiredService<INlpRuntime>();
		var parser = provider.GetRequiredService<INlpParser>();

		// ACT
		var document = runtime.Process("Open the door.");
		var parsed = parser.Parse(document);

		// ASSERT
		Assert.Equal(
			["open", "the", "door"],
			parsed.Tokens);

		Assert.Equal("open the door", parsed.NormalizedText);
	}

	[Fact]
	public void Parser_Preserves_InternalWordPunctuation()
	{
		// ARRANGE
		var services = new ServiceCollection();

		services.AddNlpRuntime(o =>
		{
			o.DataPath = "catalyst-data";
			o.Language = Language.English;
		});

		using var provider = services.BuildServiceProvider();

		var runtime = provider.GetRequiredService<INlpRuntime>();
		var parser = provider.GetRequiredService<INlpParser>();

		// ACT
		var document = runtime.Process("Use the x-ray machine.");
		var parsed = parser.Parse(document);

		// ASSERT
		Assert.Contains("x-ray", parsed.Tokens);
	}
}
