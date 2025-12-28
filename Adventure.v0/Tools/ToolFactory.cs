using Adventure.Entities;

namespace Adventure.Tools;

internal class ToolFactory
{
	public IEnumerable<IActorTool> CreateTools(Actor actor, IEnumerable<string>? toolNames)
	{
		if (toolNames == null)
		{
			yield break;
		}

		foreach (var toolName in toolNames)
		{
			yield return CreateTool(actor, toolName);
		}
	}

	public IActorTool CreateTool(Actor actor, string toolName)
	{
		return toolName switch
		{
			// GetWeatherTool.NAME => new GetWeatherTool(),
			GetShopInventoryTool.NAME => new GetShopInventoryTool(actor),
			DescribeItemTool.NAME => new DescribeItemTool(actor),
			_ => throw new ArgumentException($"Unknown tool: {toolName}", nameof(toolName)),
		};
	}
}