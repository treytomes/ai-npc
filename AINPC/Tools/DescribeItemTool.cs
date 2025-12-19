using System.ComponentModel;
using AINPC.Entities;
using AINPC.ValueObjects;

namespace AINPC.Tools;

[DisplayName(NAME)]
[Description("Describe one or more items currently for sale.")]
internal sealed class DescribeItemTool : IActorTool
{
	#region Constants

	public const string NAME = "describe_item";
	public const string INTENT = "item.describe";

	#endregion

	#region Fields

	private readonly Actor _actor;

	#endregion

	#region Constructors

	public DescribeItemTool(Actor actor)
	{
		_actor = actor ?? throw new ArgumentNullException(nameof(actor));
	}

	#endregion

	#region Properties

	public string Name => NAME;
	public string Intent => INTENT;

	#endregion

	#region Methods

	public Task<string> InvokeAsync(ToolInvocationContext context)
	{
		if (context.ResolvedItemResults == null)
			return Task.FromResult("You don't carry anything like that.");

		var items = context.ResolvedItemResults
			.Where(r => r.Item != null)
			.Select(r => r.Item!)
			.ToList();

		if (items.Count == 0)
			return Task.FromResult("You don't carry anything like that.");

		if (items.Count == 1)
		{
			var item = items[0];
			return Task.FromResult(
				$"{item.Name}: {item.Description} Costs {item.Cost}."
			);
		}

		// Multiple items
		var lines = items.Select(item =>
			$"{item.Name}: {item.Description} Costs {item.Cost}.");

		return Task.FromResult(string.Join(" ", lines));
	}

	#endregion
}
