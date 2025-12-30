using Adventure.Renderables;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL.Renderables;

internal sealed class HelpRenderable : Adventure.Renderables.Renderable
{
	public override IEnumerator<IRenderable> GetEnumerator()
	{
		yield return new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("[yellow]Command[/]")
			.AddColumn("[yellow]Description[/]")
			.AddRow(":exit", "Exit the application")
			.AddRow(":clear", "Clear the screen")
			.AddRow(":history", "Show conversation history")
			.AddRow(":plugins", "Show loaded plugins and functions")
			.AddRow(":debug", "Toggle debug mode")
			.AddRow(":config", "Show current configuration")
			.AddRow(":reload", "Reload prompt templates")
			.AddRow(":rooms", "List all available rooms")
			.AddRow(":goto <room>", "Change to a different room")
			.AddRow(":room", "Show current room details")
			.AddRow(":help", "Show this help");

		yield return new NewLineRenderable();

		yield return new Panel(
				"[cyan]General:[/]\n" +
				"  look around - Full room description\n" +
				"  look - Full room description\n\n" +
				"[cyan]Focused:[/]\n" +
				"  smell the air - Describe only smells\n" +
				"  listen carefully - Describe only sounds\n" +
				"  examine furniture - Describe only furniture\n" +
				"  inspect the door - Describe only the door\n" +
				"  look at the lighting - Describe only lighting"
			)
			.Header("[yellow]Example Commands[/]")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Yellow);

		yield return new NewLineRenderable();
	}
}