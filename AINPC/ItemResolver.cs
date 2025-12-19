using AINPC.Enums;
using AINPC.ValueObjects;
using System.Text;

namespace AINPC;

internal sealed class ItemResolver : IItemResolver
{
	private const double TokenOverlapThreshold = 0.6;
	private const int MaxEditDistance = 2;

	public ItemResolutionResult Resolve(string userInput, IReadOnlyCollection<ItemInfo> inventory)
	{
		if (string.IsNullOrWhiteSpace(userInput))
			return NotFound();

		var normalizedInput = Normalize(userInput);

		// 1. Exact name match
		var exact = inventory
			.Where(i => Normalize(i.Name) == normalizedInput)
			.ToList();

		if (exact.Count == 1)
			return Exact(exact[0]);

		if (exact.Count > 1)
			return Ambiguous(exact);

		// 2. Alias match
		var aliasMatches = inventory
			.Where(i => i.Aliases.Any(a => Normalize(a) == normalizedInput))
			.ToList();

		if (aliasMatches.Count == 1)
			return Alias(aliasMatches[0]);

		if (aliasMatches.Count > 1)
			return Ambiguous(aliasMatches);

		// 3. Token overlap (safe fuzzy)
		var tokenMatches = inventory
			.Select(i => new
			{
				Item = i,
				Score = TokenOverlapScore(normalizedInput, Normalize(i.Name))
			})
			.Where(x => x.Score >= TokenOverlapThreshold)
			.OrderByDescending(x => x.Score)
			.ToList();

		if (tokenMatches.Count == 1)
			return Fuzzy(tokenMatches[0].Item);

		if (tokenMatches.Count > 1)
			return Ambiguous(tokenMatches.Select(x => x.Item).ToList());

		// 4. Edit distance (last resort)
		var editDistanceMatches = inventory
			.Select(i => new
			{
				Item = i,
				Distance = Levenshtein(normalizedInput, Normalize(i.Name))
			})
			.Where(x => x.Distance <= MaxEditDistance)
			.OrderBy(x => x.Distance)
			.ToList();

		if (editDistanceMatches.Count == 1)
			return Fuzzy(editDistanceMatches[0].Item);

		if (editDistanceMatches.Count > 1)
			return Ambiguous(editDistanceMatches.Select(x => x.Item).ToList());

		return NotFound();
	}

	private static string Normalize(string value)
	{
		return value
			.ToLowerInvariant()
			.Normalize(NormalizationForm.FormC)
			.Trim();
	}

	private static double TokenOverlapScore(string input, string target)
	{
		var inputTokens = Tokenize(input);
		var targetTokens = Tokenize(target);

		if (inputTokens.Count == 0)
			return 0;

		var overlap = inputTokens.Intersect(targetTokens).Count();
		return overlap / (double)inputTokens.Count;
	}

	private static HashSet<string> Tokenize(string value)
	{
		return value
			.Split(' ', StringSplitOptions.RemoveEmptyEntries)
			.Select(t => t.Trim())
			.Where(t => t.Length > 0)
			.ToHashSet();
	}

	private static int Levenshtein(string a, string b)
	{
		var costs = new int[b.Length + 1];

		for (int j = 0; j < costs.Length; j++)
			costs[j] = j;

		for (int i = 1; i <= a.Length; i++)
		{
			costs[0] = i;
			int nw = i - 1;

			for (int j = 1; j <= b.Length; j++)
			{
				int cj = Math.Min(
					1 + Math.Min(costs[j], costs[j - 1]),
					a[i - 1] == b[j - 1] ? nw : nw + 1);

				nw = costs[j];
				costs[j] = cj;
			}
		}

		return costs[b.Length];
	}

	private static ItemResolutionResult Exact(ItemInfo item) =>
		new()
		{
			Status = ItemResolutionStatus.ExactMatch,
			Item = item
		};

	private static ItemResolutionResult Alias(ItemInfo item) =>
		new()
		{
			Status = ItemResolutionStatus.SingleAliasMatch,
			Item = item
		};

	private static ItemResolutionResult Fuzzy(ItemInfo item) =>
		new()
		{
			Status = ItemResolutionStatus.SingleFuzzyMatch,
			Item = item
		};

	private static ItemResolutionResult Ambiguous(IReadOnlyList<ItemInfo> items) =>
		new()
		{
			Status = ItemResolutionStatus.Ambiguous,
			Candidates = items
		};

	private static ItemResolutionResult NotFound() =>
		new()
		{
			Status = ItemResolutionStatus.NotFound
		};
}
