using System.ComponentModel;
using AINPC.Entities;
using AINPC.ValueObjects;

namespace AINPC.Tools;

[DisplayName(NAME)]
[Description("List all items currently for sale in the shop, including prices.")]
internal class DescribeItemTool : IActorTool
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

	public async Task<string> InvokeAsync(ToolInvocationContext context)
	{
		var item = context.ResolvedItem;
		if (item == null)
			return await Task.FromResult("You don't carry that.");

		return await Task.FromResult($"{item.Name} is described as \"{item.Description}\"");
	}
	#endregion
}
