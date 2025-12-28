using Adventure.NLP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Adventure.NLP.Test;

/// <summary>
/// Verifies that the NLP parser converts processed documents into
/// normalized <see cref="ParsedInput"/> instances.
/// </summary>
public sealed class NlpParserTests
{
	private readonly IServiceProvider _provider;
	private readonly INlpRuntime _runtime;
	private readonly INlpParser _parser;

	public NlpParserTests()
	{
		var services = new ServiceCollection();
		services.AddNlpRuntime();

		_provider = services.BuildServiceProvider();

		_runtime = _provider.GetRequiredService<INlpRuntime>();
		_parser = _provider.GetRequiredService<INlpParser>();
	}

	[Fact]
	public void Parser_Produces_NormalizedParsedInput()
	{
		const string input = "Open the door.";

		var document = _runtime.Process(input);
		var parsed = _parser.Parse(document);

		Assert.Equal("Open the door.", parsed.RawText);
		Assert.Equal("open the door", parsed.NormalizedText);

		Assert.Equal(
			new[] { "open", "the", "door" },
			parsed.Tokens);
	}
}
