using AINPC.ValueObjects;

namespace AINPC.Models;

class Inventory
{
	public int Gold { get; set; }
	public List<ItemInfo> Items { get; } = new();
}
