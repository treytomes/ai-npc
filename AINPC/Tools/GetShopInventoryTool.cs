using AINPC.Entities;
using AINPC.ValueObjects;
using System.ComponentModel;
using System.Text;

namespace AINPC.Tools;

/// <summary>
/// Tool used by a shopkeeper to list items currently available for sale.
/// </summary>
[DisplayName(NAME)]
[Description("List all items currently for sale in the shop, including prices.")]
internal class GetShopInventoryTool : IActorTool
{
	#region Constants

	public const string NAME = "list_shop_inventory";
	public const string INTENT = "shop.inventory.list";

	#endregion

	#region Fields

	private readonly Actor _actor;

	#endregion

	#region Constructors

	public GetShopInventoryTool(Actor actor)
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
		if (_actor.Inventory.Count == 0)
		{
			return "No items are currently for sale.";
		}

		var sb = new StringBuilder();

		// Intentionally structured, not flowery.
		for (var i = 0; i < _actor.Inventory.Count; i++)
		{
			var item = _actor.Inventory[i];
			sb.AppendLine($"{i + 1}. {item.Name} - {item.Cost} gold");
		}

		return await Task.FromResult(sb.ToString());
	}
	#endregion
}
