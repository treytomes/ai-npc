using Adventure.LLM.REPL.Configuration;
using Adventure.Renderables;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL.Renderables;

internal sealed class ConfigurationRenderable(AppConfiguration config) : Adventure.Renderables.Renderable
{
	private readonly AppConfiguration _config = config ?? throw new ArgumentNullException(nameof(config));

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[yellow]Current Configuration[/]")
			.AddColumn("[cyan]Setting[/]")
			.AddColumn("[cyan]Value[/]");

		table.AddRow("Sentence Count", _config.Rendering.SentenceCount);
		table.AddRow("Temperature", _config.Rendering.Temperature.ToString());
		table.AddRow("Max Tokens", _config.Rendering.MaxTokens.ToString());
		table.AddRow("Max Validation Attempts", _config.Validation.MaxAttempts.ToString());
		table.AddRow("Min Sentences", _config.Validation.MinSentences);
		table.AddRow("Max Sentences", _config.Validation.MaxSentences);

		yield return table;
		yield return new NewLineRenderable();
	}
}