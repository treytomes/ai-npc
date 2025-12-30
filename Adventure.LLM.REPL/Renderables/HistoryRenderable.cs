using Adventure.Renderables;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL.Renderables;

internal sealed class HistoryRenderable(IEnumerable<ChatMessageContent> history) : Adventure.Renderables.Renderable
{
	private readonly IEnumerable<ChatMessageContent> _history = history ?? throw new ArgumentNullException(nameof(history));

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		if (!_history.Any())
		{
			yield return new Markup("[grey]No history yet.[/]");
			yield return new NewLineRenderable();
			yield break;
		}

		yield return new Panel("[grey]Conversation History[/]")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Grey);

		foreach (var message in _history.Skip(1)) // Skip system prompt.
		{
			var role = message.Role == AuthorRole.User
				? "[blue]User[/]"
				: "[green]Assistant[/]";
			yield return new Markup($"{role}: {Markup.Escape(message.Content ?? "")}");
			yield return new NewLineRenderable();
		}

		yield return new NewLineRenderable();
	}
}