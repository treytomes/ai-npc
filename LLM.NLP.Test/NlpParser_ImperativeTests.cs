using LLM.NLP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LLM.NLP.Test;

/// <summary>
/// Integration-style tests for imperative command parsing.
/// Validates runtime + parser behavior together.
/// </summary>
public sealed class NlpParser_ImperativeTests
{
	private readonly IServiceProvider _provider;
	private readonly INlpRuntime _runtime;
	private readonly INlpParser _parser;

	public NlpParser_ImperativeTests()
	{
		var services = new ServiceCollection();
		services.AddNlpRuntime();

		_provider = services.BuildServiceProvider();

		_runtime = _provider.GetRequiredService<INlpRuntime>();
		_parser = _provider.GetRequiredService<INlpParser>();
	}

	[Theory]
	[InlineData("open door")]
	[InlineData("take the sword")]
	[InlineData("look around")]
	public void Parser_Handles_Imperative_Commands(string input)
	{
		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		Assert.NotEmpty(parsed.Lemmas);
	}

	[Fact]
	public void Parser_Preserves_Stopwords_In_Lemmas()
	{
		var input = "open the door";

		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		Assert.Contains("the", parsed.Lemmas);
	}
}
