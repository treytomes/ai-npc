using LLM.Intent.Classification.Facts;
using NRules;

namespace LLM.Intent.Classification;

internal sealed class HighestConfidenceIntentAggregator : IIntentAggregator
{
	public IReadOnlyList<Intent> Aggregate(ISession session)
	{
		var suppressed = session.Query<SuppressedIntent>()
			.Select(s => s.Name)
			.ToHashSet();

		return session.Query<Intent>()
			.Where(i => !suppressed.Contains(i.Name))
			.GroupBy(i => i, new IntentEqualityComparer())
			.Select(g => g.MaxBy(i => i.Confidence)!)
			.OrderByDescending(i => i.Confidence)
			.ToList();
	}

	private sealed class IntentEqualityComparer : IEqualityComparer<Intent>
	{
		public bool Equals(Intent? x, Intent? y)
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

		public int GetHashCode(Intent obj)
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
}
