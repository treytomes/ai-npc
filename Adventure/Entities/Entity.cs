namespace Adventure.Entities;

internal abstract class Entity
{
	public string GetId()
	{
		return $"<{GetType().Name}>{GetHashCode()}";
	}
}