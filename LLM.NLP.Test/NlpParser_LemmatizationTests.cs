using LLM.NLP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LLM.NLP.Test;

/// <summary>
/// Integration tests for lemmatization behavior.
/// Verifies that verbs and nouns are normalized correctly.
/// </summary>
public sealed class NlpParser_LemmatizationTests
{
	private readonly IServiceProvider _provider;
	private readonly INlpRuntime _runtime;
	private readonly INlpParser _parser;

	public NlpParser_LemmatizationTests()
	{
		var services = new ServiceCollection();
		services.AddNlpRuntime();

		_provider = services.BuildServiceProvider();

		_runtime = _provider.GetRequiredService<INlpRuntime>();
		_parser = _provider.GetRequiredService<INlpParser>();
	}

	[Fact]
	public void Parser_Lemmatizes_Verbs_And_Nouns()
	{
		const string input = "The doors were opened.";

		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		Assert.Contains("door", parsed.Lemmas);
		Assert.Contains("open", parsed.Lemmas);
	}
}
