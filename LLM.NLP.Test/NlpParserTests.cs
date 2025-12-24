using Microsoft.Extensions.DependencyInjection;

namespace LLM.NLP.Test;

/// <summary>
/// Verifies that the NLP parser converts processed documents into
/// normalized ParsedInput instances.
/// </summary>
public class NlpParserTests
{
	[Fact]
	public void Parser_Produces_NormalizedParsedInput()
	{
		// ARRANGE
		var services = new ServiceCollection();

		services.AddNlpRuntime(options =>
		{
			options.DataPath = "catalyst-data";
			options.Language = Mosaik.Core.Language.English;
		});

		using var provider = services.BuildServiceProvider();

		var runtime = provider.GetRequiredService<INlpRuntime>();
		var parser = provider.GetRequiredService<INlpParser>();

		var document = runtime.Process("Open the door.");

		// ACT
		var parsed = parser.Parse(document);

		// ASSERT
		Assert.Equal("Open the door.", parsed.RawText);
		Assert.Equal("open the door", parsed.NormalizedText);

		Assert.Equal(
			["open", "the", "door"],
			parsed.Tokens);
	}
}
