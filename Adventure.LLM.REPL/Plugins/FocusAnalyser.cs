internal sealed class FocusAnalyzer
{
	private static readonly Dictionary<string, (string focus, string dataPath)> FocusKeywords = new()
	{
		// Sensory focuses
		["smell"] = ("smells and odors", "spatial_summary.smell"),
		["sniff"] = ("smells and odors", "spatial_summary.smell"),
		["odor"] = ("smells and odors", "spatial_summary.smell"),
		["sound"] = ("sounds and ambient noise", "ambient_details.always"),
		["listen"] = ("sounds and ambient noise", "ambient_details.always"),
		["hear"] = ("sounds and ambient noise", "ambient_details.always"),
		["light"] = ("lighting conditions", "spatial_summary.lighting"),
		["dark"] = ("lighting conditions", "spatial_summary.lighting"),
		["bright"] = ("lighting conditions", "spatial_summary.lighting"),

		// Object focuses
		["furniture"] = ("furniture", "static_features[type=furniture]"),
		["table"] = ("furniture", "static_features[type=furniture]"),
		["chair"] = ("furniture", "static_features[type=furniture]"),
		["shelf"] = ("shelving", "static_features[type=shelving]"),
		["door"] = ("doors", "static_features[type=door]"),
		["exit"] = ("exits and doors", "static_features[type=door]"),

		// Detail focuses
		["examine"] = ("detailed examination", ""),
		["inspect"] = ("close inspection", ""),
		["detail"] = ("fine details", ""),
	};

	public static (string focus, bool isSpecific) DetermineFocus(string userInput)
	{
		var lowerInput = userInput.ToLower();

		// Check for specific object mentions
		foreach (var (keyword, (focus, dataPath)) in FocusKeywords)
		{
			if (lowerInput.Contains(keyword))
			{
				// If it's an examine/inspect command, look for what they're examining
				if (keyword is "examine" or "inspect" or "detail")
				{
					// Try to find what they're examining
					var targetFocus = ExtractExaminationTarget(lowerInput);
					if (!string.IsNullOrEmpty(targetFocus))
					{
						return (targetFocus, true);
					}
				}

				return (focus, true);
			}
		}

		// Check for general look commands
		if (lowerInput.Contains("look") && !lowerInput.Contains("look at"))
		{
			return (string.Empty, false); // General room description
		}

		return (string.Empty, false);
	}

	private static string ExtractExaminationTarget(string input)
	{
		// Simple extraction - look for "examine the X" or "inspect X"
		var patterns = new[] { "examine the ", "examine ", "inspect the ", "inspect ", "look at the ", "look at " };

		foreach (var pattern in patterns)
		{
			var index = input.IndexOf(pattern);
			if (index >= 0)
			{
				var target = input.Substring(index + pattern.Length).Trim();
				// Take first word or two as the target
				var words = target.Split(' ').Take(2);
				return string.Join(" ", words);
			}
		}

		return string.Empty;
	}
}