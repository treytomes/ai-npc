namespace AINPC.Models;

record CharacterInfo
{
	public string Name { get; init; }
	public IReadOnlyCollection<string> PersonalityTraits { get; init; }
}
