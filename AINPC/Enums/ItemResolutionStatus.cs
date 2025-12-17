namespace AINPC.Enums;

public enum ItemResolutionStatus
{
	ExactMatch,
	SingleAliasMatch,
	SingleFuzzyMatch,
	Ambiguous,
	NotFound
}