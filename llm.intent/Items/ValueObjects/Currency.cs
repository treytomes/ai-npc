namespace LLM.Intent.Items.ValueObjects;

/// <remarks>Cloned from AINPC.ValueObjects.</remarks>
internal record Currency(decimal Value)
{
	public override string ToString()
	{
		return Value.ToString("C");
	}
}