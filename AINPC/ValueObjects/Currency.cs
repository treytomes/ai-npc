namespace AINPC.ValueObjects;

internal record Currency(decimal Value)
{
	public override string ToString()
	{
		return Value.ToString("C");
	}
}