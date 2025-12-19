namespace AINPC.Intent.Classification.Facts;

internal sealed class Intent(string name, double confidence)
{
	#region Properties

	public string Name { get; } = name;
	public double Confidence { get; } = confidence;
	public Dictionary<string, string> Slots { get; } = new();

	#endregion

	#region Methods

	public Intent WithSlot(string slotName, string slotValue)
	{
		Slots[slotName] = slotValue;
		return this;
	}

	public bool HasSlot(string slotName) => Slots.ContainsKey(slotName);

	#endregion
}
