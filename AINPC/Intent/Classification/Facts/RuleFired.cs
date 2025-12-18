namespace AINPC.Intent.Classification.Facts;

public sealed class RuleFired
{
	public string RuleName { get; }

	public RuleFired(string ruleName)
	{
		RuleName = ruleName;
	}
}
