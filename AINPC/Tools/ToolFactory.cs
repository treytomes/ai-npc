using AINPC.Entities;

namespace AINPC.Tools;

internal class ToolFactory
{
	public IEnumerable<IOllamaTool> CreateTools(Actor actor, IEnumerable<string>? toolNames)
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

	public IOllamaTool CreateTool(Actor actor, string toolName)
	{
		return toolName switch
		{
			GetWeatherTool.NAME => new GetWeatherTool(),
			GetShopInventoryTool.NAME => new GetShopInventoryTool(actor),
			_ => throw new ArgumentException($"Unknown tool: {toolName}", nameof(toolName)),
		};
	}
}