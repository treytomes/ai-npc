using System.ComponentModel;
using Adventure.LLM.REPL.Templating;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Adventure.LLM.REPL.Plugins;


internal sealed class RoomValidatorPlugin
{
	private readonly string _promptRootPath;
	private readonly IChatCompletionService _chatService;
	private readonly Kernel _kernel;
	private readonly ILogger<RoomValidatorPlugin> _logger;
	private readonly KernelFunction _validateFunction;

	public RoomValidatorPlugin(
		string promptRootPath,
		Kernel kernel,
		ILogger<RoomValidatorPlugin> logger)
	{
		_promptRootPath = promptRootPath;
		_kernel = kernel;
		_chatService = kernel.GetRequiredService<IChatCompletionService>();
		_logger = logger;

		// Load and create the validation function from template
		_validateFunction = CreateValidateFunction();
	}

	private KernelFunction CreateValidateFunction()
	{
		var template = LoadPromptTemplate(Path.Combine(_promptRootPath, "room_validator.yaml"));

		var promptConfig = new PromptTemplateConfig
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
				["default"] = new()
				{
					ExtensionData = new Dictionary<string, object>
					{
						["temperature"] = template.ExecutionSettings.Temperature,
						["max_tokens"] = template.ExecutionSettings.MaxTokens
					}
				}
			}
		};

		var factory = new HandlebarsPromptTemplateFactory();
		var promptTemplate = factory.Create(promptConfig);

		return KernelFunctionFactory.CreateFromPrompt(
			promptTemplate,
			promptConfig,
			_kernel.LoggerFactory);
	}

	private static PromptTemplate LoadPromptTemplate(string filename)
	{
		var yamlContent = File.ReadAllText(filename);
		var deserializer = new DeserializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		return deserializer.Deserialize<PromptTemplate>(yamlContent);
	}

	[KernelFunction("ValidateRoomDescription")]
	[Description("Validates a room description meets requirements")]
	[return: Description("Returns true if valid, false if regeneration needed")]
	public async Task<bool> ValidateRoomDescriptionAsync(
		[Description("The room description to validate")] string description,
		[Description("Minimum sentences")] string minSentences = "3",
		[Description("Maximum sentences")] string maxSentences = "5")
	{
		var arguments = new KernelArguments
		{
			["description"] = description,
			["minSentences"] = minSentences,
			["maxSentences"] = maxSentences
		};

		var response = await _kernel.InvokeAsync<string>(_validateFunction, arguments);
		var verdict = response?.Trim();

		_logger.LogInformation("Validator verdict: {Verdict}", verdict);

		return verdict == "OK";
	}
}
