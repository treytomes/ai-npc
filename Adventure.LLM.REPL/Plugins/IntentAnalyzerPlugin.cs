using System.ComponentModel;
using System.Text.RegularExpressions;
using Adventure.LLM.REPL.Templating;
using Adventure.LLM.REPL.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Polly;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Adventure.LLM.REPL.Plugins;

internal sealed class IntentAnalyzerPlugin
{
	#region Fields

	private readonly string _promptRootPath;
	private readonly Kernel _kernel;
	private readonly ILogger<IntentAnalyzerPlugin> _logger;
	private readonly KernelFunction _analyzeFunction;
	private readonly IAsyncPolicy<UserIntent> _retryPolicy;

	// Regex for parsing the response.
	private static readonly Regex IntentPattern = new(@"intent:\s*(\w+)", RegexOptions.IgnoreCase);
	private static readonly Regex FocusPattern = new(@"focus:\s*(.*)$", RegexOptions.IgnoreCase | RegexOptions.Multiline);

	#endregion

	#region Constructors

	public IntentAnalyzerPlugin(
		string promptRootPath,
		Kernel kernel,
		ILogger<IntentAnalyzerPlugin> logger)
	{
		_promptRootPath = promptRootPath;
		_kernel = kernel;
		_logger = logger;
		_analyzeFunction = CreateAnalyzeFunction();

		// Configure Polly retry policy
		_retryPolicy = Policy<UserIntent>
			.HandleResult(result => !result.IsValid)
			.WaitAndRetryAsync(
				3, // Retry up to 3 times
				retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), // Exponential backoff
				onRetry: (outcome, timespan, retryCount, context) =>
				{
					_logger.LogWarning(
						"Intent analysis retry {RetryCount} after {Timespan}ms. Previous error: {Error}",
						retryCount,
						timespan.TotalMilliseconds,
						outcome.Result?.Error ?? "Unknown error");
				});
	}

	#endregion

	#region Methods

	private KernelFunction CreateAnalyzeFunction()
	{
		var template = LoadPromptTemplate(Path.Combine(_promptRootPath, "intent_analyzer.yaml"));

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

	[KernelFunction("AnalyzeIntent")]
	[Description("Analyzes user input to extract intent and focus")]
	public async Task<UserIntent> AnalyzeIntentAsync(
		[Description("User input to analyze")] string userInput)
	{
		return await _retryPolicy.ExecuteAsync(async () =>
		{
			try
			{
				var arguments = new KernelArguments
				{
					["userInput"] = userInput
				};

				var response = await _kernel.InvokeAsync<string>(_analyzeFunction, arguments);

				if (string.IsNullOrWhiteSpace(response))
				{
					_logger.LogWarning("Empty response from intent analyzer");
					return CreateFallbackIntent(userInput, "Empty response from AI");
				}

				// Parse the response
				var intent = ParseIntent(response, userInput);

				// Validate the intent
				if (!IntentTypes.ValidIntents.Contains(intent.Intent))
				{
					_logger.LogWarning("Invalid intent detected: {Intent}", intent.Intent);
					intent.Intent = DetermineFallbackIntent(userInput);
				}

				_logger.LogInformation(
					"Analyzed intent - Input: '{Input}' → Intent: '{Intent}', Focus: '{Focus}'",
					userInput,
					intent.Intent,
					intent.Focus);

				return intent;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error analyzing intent for input: {Input}", userInput);
				return CreateFallbackIntent(userInput, ex.Message);
			}
		});
	}

	private UserIntent ParseIntent(string response, string originalInput)
	{
		var intent = new UserIntent
		{
			OriginalInput = originalInput,
			IsValid = true
		};

		// Extract intent
		var intentMatch = IntentPattern.Match(response);
		if (intentMatch.Success)
		{
			intent.Intent = intentMatch.Groups[1].Value.ToLower().Trim();
		}
		else
		{
			intent.Intent = IntentTypes.Unknown;
			intent.IsValid = false;
			intent.Error = "Could not parse intent from response";
		}

		// Extract focus
		var focusMatch = FocusPattern.Match(response);
		if (focusMatch.Success)
		{
			var focusValue = focusMatch.Groups[1].Value.Trim();
			// Remove quotes if present
			focusValue = focusValue.Trim('"', '\'');
			intent.Focus = focusValue;
		}

		return intent;
	}

	private UserIntent CreateFallbackIntent(string userInput, string error)
	{
		return new UserIntent
		{
			OriginalInput = userInput,
			Intent = DetermineFallbackIntent(userInput),
			Focus = ExtractFallbackFocus(userInput),
			IsValid = false,
			Error = error
		};
	}

	private string DetermineFallbackIntent(string userInput)
	{
		var lower = userInput.ToLower();

		// Simple keyword matching as fallback
		if (lower.Contains("examine") || lower.Contains("inspect") || lower.Contains("check"))
			return IntentTypes.Examine;
		if (lower.Contains("go") || lower.Contains("walk") || lower.Contains("move") || lower.Contains("head"))
			return IntentTypes.Go;
		if (lower.Contains("take") || lower.Contains("get") || lower.Contains("grab") || lower.Contains("pick"))
			return IntentTypes.Take;
		if (lower.Contains("use") || lower.Contains("activate") || lower.Contains("press") || lower.Contains("pull"))
			return IntentTypes.Use;
		if (lower.Contains("smell") || lower.Contains("sniff"))
			return IntentTypes.Smell;
		if (lower.Contains("listen") || lower.Contains("hear"))
			return IntentTypes.Listen;
		if (lower.Contains("touch") || lower.Contains("feel"))
			return IntentTypes.Touch;
		if (lower.Contains("look"))
			return IntentTypes.Look;

		return IntentTypes.Look; // Default to look
	}

	private string ExtractFallbackFocus(string userInput)
	{
		// Simple extraction of potential focus words
		var words = userInput.ToLower().Split(' ');
		var skipWords = new HashSet<string> { "the", "a", "an", "at", "to", "around", "examine", "inspect", "look", "go", "walk" };

		var focusWords = words.Where(w => !skipWords.Contains(w)).Take(2);
		return string.Join(" ", focusWords);
	}

	[KernelFunction("GetIntentDescription")]
	[Description("Gets a description of what an intent means")]
	public string GetIntentDescription(
		[Description("The intent type")] string intent)
	{
		return intent switch
		{
			IntentTypes.Look => "General observation of the environment",
			IntentTypes.Examine => "Detailed inspection of a specific object or area",
			IntentTypes.Go => "Movement to a different location",
			IntentTypes.Take => "Picking up or acquiring an item",
			IntentTypes.Use => "Interacting with or activating an object",
			IntentTypes.Smell => "Perceiving odors and scents",
			IntentTypes.Listen => "Focusing on sounds and audio",
			IntentTypes.Touch => "Feeling textures and physical sensations",
			_ => "Unknown action"
		};
	}

	#endregion
}
