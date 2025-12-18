namespace AINPC.Intent.Classification;

internal sealed class IntentEqualityComparer : IEqualityComparer<Facts.Intent>
{
	public bool Equals(Facts.Intent? x, Facts.Intent? y)
	{
		if (ReferenceEquals(x, y)) return true;
		if (x is null || y is null) return false;

		// Check name
		if (x.Name != y.Name) return false;

		// Check slots
		if (x.Slots.Count != y.Slots.Count) return false;

		foreach (var kvp in x.Slots)
		{
			if (!y.Slots.TryGetValue(kvp.Key, out var yValue) ||
				kvp.Value != yValue)
			{
				return false;
			}
		}

		return true;
	}

	public int GetHashCode(Facts.Intent obj)
	{
		var hash = new HashCode();
		hash.Add(obj.Name);

		// Add slots in deterministic order
		foreach (var kvp in obj.Slots.OrderBy(x => x.Key))
		{
			hash.Add(kvp.Key);
			hash.Add(kvp.Value);
		}

		return hash.ToHashCode();
	}
}
