namespace AINPC.Models;

class PlayerInventory
{
	public int Gold { get; set; }
	public List<Item> Items { get; } = new();
}
