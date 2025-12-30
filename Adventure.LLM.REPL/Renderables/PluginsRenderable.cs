using Adventure.Renderables;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL.Renderables;

internal sealed class PluginsRenderable(IEnumerable<KernelPlugin> plugins) : Adventure.Renderables.Renderable
{
	private readonly IEnumerable<KernelPlugin> _plugins = plugins ?? throw new ArgumentNullException(nameof(plugins));

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[yellow]Loaded Plugins[/]")
			.AddColumn("[cyan]Plugin[/]")
			.AddColumn("[cyan]Functions[/]")
			.AddColumn("[cyan]Template[/]");

		foreach (var plugin in _plugins)
		{
			var functions = string.Join("\n", plugin.Select(f => f.Name));
			var templateFile = plugin.Name switch
			{
				"IntentAnalyzer" => "intent_analyzer.yaml",
				"RoomRenderer" => "room_renderer.yaml",
				"RoomValidator" => "room_validator.yaml",
				_ => "N/A"
			};
			table.AddRow(plugin.Name, functions, templateFile);
		}

		yield return table;
		yield return new NewLineRenderable();
	}
}