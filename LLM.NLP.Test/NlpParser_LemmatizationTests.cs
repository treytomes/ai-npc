using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;

namespace LLM.NLP.Test;

public class NlpParser_LemmatizationTests
{
	[Fact]
	public void Parser_Lemmatizes_Verbs_And_Nouns()
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
		var document = runtime.Process("The doors were opened.");
		var parsed = parser.Parse(document);

		// ASSERT
		Assert.Contains("door", parsed.Lemmas);
		Assert.Contains("open", parsed.Lemmas);
	}

}