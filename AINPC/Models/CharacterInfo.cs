namespace AINPC.Models;

record CharacterInfo
{
	public required string Name { get; init; }
	public required IReadOnlyCollection<string> PersonalityTraits { get; init; }
}
