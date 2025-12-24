using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;

namespace LLM.NLP.Test;

public class NlpParser_ImperativeTests
{
	[Theory]
	[InlineData("open door", new[] { "open", "door" })]
	[InlineData("take the sword", new[] { "take", "the", "sword" })]
	[InlineData("look around", new[] { "look", "around" })]
	public void Parser_Handles_Imperative_Commands(
		string input,
		string[] expectedLemmas)
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
		var document = runtime.Process(input);
		var parsed = parser.Parse(document);

		// ASSERT
		Assert.Equal(expectedLemmas, parsed.Lemmas);
	}

	[Fact]
	public void Parser_Preserves_Stopwords_In_Lemmas()
	{
		// ARRANGE
		var input = "open the door";
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
		var document = runtime.Process(input);
		var parsed = parser.Parse(document);

		Assert.Contains("the", parsed.Lemmas);
	}
}
