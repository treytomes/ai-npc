using LLM.NLP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LLM.NLP.Test;

/// <summary>
/// Regression tests to ensure punctuation handling remains stable
/// during NLP parsing.
/// </summary>
public sealed class NlpParser_PunctuationTests
{
	private readonly IServiceProvider _provider;
	private readonly INlpRuntime _runtime;
	private readonly INlpParser _parser;

	public NlpParser_PunctuationTests()
	{
		var services = new ServiceCollection();
		services.AddNlpRuntime();

		_provider = services.BuildServiceProvider();

		_runtime = _provider.GetRequiredService<INlpRuntime>();
		_parser = _provider.GetRequiredService<INlpParser>();
	}

	[Fact]
	public void Parser_Removes_TerminalPunctuation()
	{
		const string input = "Open the door.";

		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		Assert.Equal(
			["open", "the", "door"],
			parsed.Tokens);

		Assert.Equal("open the door", parsed.NormalizedText);
	}

	[Fact]
	public void Parser_Preserves_InternalWordPunctuation()
	{
		const string input = "Use the x-ray machine.";

		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		Assert.Contains("x-ray", parsed.Tokens);
	}
}
