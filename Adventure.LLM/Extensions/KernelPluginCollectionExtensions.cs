using Microsoft.SemanticKernel;

namespace Adventure.LLM;

public static class KernelPluginCollectionExtensions
{
	public static bool Remove(this KernelPluginCollection @this, params string[] pluginNames)
	{
		var plugins = @this.Where(x => pluginNames.Contains(x.Name));
		if (plugins == null || plugins.Count() == 0)
		{
			return false;
		}
		var success = true;
		foreach (var plugin in plugins)
		{
			if (!@this.Remove(plugin))
			{
				success = false;
			}
		}
		return success;
	}
}