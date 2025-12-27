using Catalyst;
using LLM.NLP.REPL.Renderers;
using LLM.NLP.Services;
using LLM.NLP.Test.Helpers;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;
using Spectre.Console;

namespace LLM.NLP.Test;

public sealed class IntentSeedExtractor_PrepositionsTests : IDisposable
{
	private readonly ServiceProvider _provider;
	private readonly IIntentSeedExtractor _extractor;

	public IntentSeedExtractor_PrepositionsTests()
	{
		var services = new ServiceCollection();

		services.AddNlpRuntime(o =>
		{
			o.DataPath = "catalyst-data";
			o.Language = Language.English;
		});

		_provider = services.BuildServiceProvider();
		_extractor = _provider.GetRequiredService<IIntentSeedExtractor>();

		AnsiConsole.WriteLine();
		AnsiConsole.Write(
			new Rule("[bold green]IntentSeedExtractor — Prepositional Phrases[/]")
				.LeftJustified());
	}

	public void Dispose()
	{
		AnsiConsole.Write(
			new Rule("[dim]End Prepositional Phrase Tests[/]")
				.LeftJustified());
		AnsiConsole.WriteLine();
	}

	[Fact]
	public void Extracts_From_Prepositional_Phrase()
	{
		var parsed = new ParsedInputBuilder()
			.Token("take", "take", PartOfSpeech.VERB)
			.Token("key", "key", PartOfSpeech.NOUN)
			.Token("from", "from", PartOfSpeech.ADP)
			.Token("chest", "chest", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render("take key from chest", parsed, seed);

		Assert.Equal("take", seed.Verb);

		Assert.NotNull(seed.DirectObject);
		Assert.Equal("key", seed.DirectObject!.Head);
		Assert.Equal("key from chest", seed.DirectObject.Text);

		Assert.Empty(seed.Prepositions);

		Assert.True(seed.DirectObject.Complements.ContainsKey("from"));
		var chest = seed.DirectObject.Complements["from"];

		Assert.Equal("chest", chest.Head);
		Assert.Equal("chest", chest.Text);
	}

	[Fact]
	public void Extracts_On_Prepositional_Phrase()
	{
		var parsed = new ParsedInputBuilder()
			.Token("put", "put", PartOfSpeech.VERB)
			.Token("book", "book", PartOfSpeech.NOUN)
			.Token("on", "on", PartOfSpeech.ADP)
			.Token("table", "table", PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render("put book on table", parsed, seed);

		Assert.Equal("put", seed.Verb);

		Assert.NotNull(seed.DirectObject);
		Assert.Equal("book", seed.DirectObject!.Head);

		Assert.Empty(seed.Prepositions);

		Assert.True(seed.DirectObject.Complements.ContainsKey("on"));
		Assert.Equal("table", seed.DirectObject.Complements["on"].Head);
	}

	[Fact]
	public void Prepositional_Object_Preserves_Modifiers()
	{
		var parsed = new ParsedInputBuilder()
			.Token("put", pos: PartOfSpeech.VERB)
			.Token("book", pos: PartOfSpeech.NOUN)
			.Token("on", pos: PartOfSpeech.ADP)
			.Token("wooden", pos: PartOfSpeech.ADJ)
			.Token("table", pos: PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render(parsed.RawText, parsed, seed);

		var table = seed.DirectObject!
			.Complements["on"];

		Assert.Equal("table", table.Head);
		Assert.Equal(["wooden"], table.Modifiers);
		Assert.Equal("wooden table", table.Text);
	}

	[Fact]
	public void Multiple_Prepositional_Phrases_Are_Extracted()
	{
		var parsed = new ParsedInputBuilder()
			.Token("take", pos: PartOfSpeech.VERB)
			.Token("key", pos: PartOfSpeech.NOUN)
			.Token("from", pos: PartOfSpeech.ADP)
			.Token("chest", pos: PartOfSpeech.NOUN)
			.Token("in", pos: PartOfSpeech.ADP)
			.Token("room", pos: PartOfSpeech.NOUN)
			.Build();

		var seed = _extractor.Extract(parsed);

		IntentSeedSnapshotRenderer.Render(parsed.RawText, parsed, seed);

		var key = seed.DirectObject!;
		Assert.True(key.Complements.ContainsKey("from"));

		var chest = key.Complements["from"];
		Assert.Equal("chest", chest.Head);

		Assert.True(chest.Complements.ContainsKey("in"));
		Assert.Equal("room", chest.Complements["in"].Head);
	}
}
