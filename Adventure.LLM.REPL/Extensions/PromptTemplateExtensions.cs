using Adventure.LLM.REPL.Templating;
using Microsoft.SemanticKernel;

namespace Adventure.LLM.REPL;

public static class PromptTemplateExtensions
{
	public static PromptTemplateConfig ToPromptTemplateConfig(this PromptTemplate template)
	{
		return new PromptTemplateConfig
		{
			Template = template.Template,
			TemplateFormat = "handlebars",
			InputVariables = template.InputVariables.Select(iv => new InputVariable
			{
				Name = iv.Name,
				Description = iv.Description,
				Default = iv.Default
			}).ToList(),
			ExecutionSettings = new Dictionary<string, PromptExecutionSettings>
			{
				["default"] = template.ExecutionSettings.ToPromptExecutionSettings()
			}
		};
	}

	public static PromptExecutionSettings ToPromptExecutionSettings(this ExecutionSettings settings)
	{
		var extensionData = new Dictionary<string, object>
		{
			["temperature"] = settings.Temperature,
			["max_tokens"] = settings.MaxTokens
		};

		if (settings.StopSequences != null && settings.StopSequences.Any())
		{
			extensionData["stop"] = settings.StopSequences;
		}

		return new PromptExecutionSettings
		{
			ExtensionData = extensionData
		};
	}
}
