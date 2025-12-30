using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Polly;
using Polly.Retry;

namespace Adventure.LLM.REPL.Plugins;

internal sealed class RoomOrchestrationPlugin
{
	#region Fields

	private readonly Kernel _kernel;
	private readonly ILogger<RoomOrchestrationPlugin> _logger;
	private readonly Dictionary<string, object> _config;
	private readonly AsyncRetryPolicy<(string Result, bool IsValid)> _retryPolicy;

	#endregion

	#region Constructors

	public RoomOrchestrationPlugin(
		Kernel kernel,
		ILogger<RoomOrchestrationPlugin> logger,
		Dictionary<string, object>? config = null)
	{
		_kernel = kernel;
		_logger = logger;
		_config = config ?? new Dictionary<string, object>
		{
			["maxAttempts"] = 2,
			["sentenceCount"] = "3-5",
			["minSentences"] = "3",
			["maxSentences"] = "5"
		};

		// Create the retry policy
		var maxAttempts = Convert.ToInt32(_config!.GetValueOrDefault("maxAttempts", 2));

		_retryPolicy = Policy
			.HandleResult<(string Result, bool IsValid)>(r => !r.IsValid)
			.WaitAndRetryAsync(
				retryCount: maxAttempts - 1, // -1 because the first attempt isn't a retry.
				sleepDurationProvider: retryAttempt => TimeSpan.FromMilliseconds(100 * retryAttempt), // Optional backoff.
				onRetry: (outcome, timespan, retryCount, context) =>
				{
					_logger.LogWarning(
						"Validation failed on attempt {Attempt}. Retrying in {Delay}ms...",
						retryCount,
						timespan.TotalMilliseconds);
				});
	}

	#endregion

	#region Methods

	[KernelFunction("RenderValidatedRoom")]
	[Description("Renders a room with automatic validation and retry")]
	public async Task<string> RenderValidatedRoomAsync(
		[Description("Room YAML data")] string roomData,
		[Description("User input")] string userInput,
		[Description("Optional focus area")] string focus = "")
	{
		// Determine if this is a focused description.
		var isSpecific = !string.IsNullOrEmpty(focus);

		// Adjust sentence count based on whether it's a focused description.
		var sentenceCount = isSpecific ? "2-3" : _config!.GetValueOrDefault("sentenceCount", "3-5")!;
		var minSentences = isSpecific ? "2" : _config!.GetValueOrDefault("minSentences", "3")!;
		var maxSentences = isSpecific ? "3" : _config!.GetValueOrDefault("maxSentences", "5")!;

		if (!string.IsNullOrEmpty(focus))
		{
			_logger.LogInformation("Detected focus area: {Focus} (Specific: {IsSpecific})", focus, isSpecific);
		}

		var context = new Context
		{
			["roomData"] = roomData,
			["userInput"] = userInput,
			["sentenceCount"] = sentenceCount,
			["minSentences"] = minSentences,
			["maxSentences"] = maxSentences
		};

		var attempt = 0;
		var result = await _retryPolicy.ExecuteAsync(
			async (ctx) =>
			{
				attempt++;
				_logger.LogInformation("Rendering attempt {Attempt}", attempt);

				// Render the room
				var rendered = await _kernel.InvokeAsync<string>(
					"RoomRenderer",
					"RenderRoom",
					new KernelArguments
					{
						["roomData"] = ctx["roomData"],
						["userInput"] = ctx["userInput"],
						["sentenceCount"] = ctx["sentenceCount"],
						["focus"] = focus,
					}) ?? string.Empty;

				// Validate the result
				var isValid = await _kernel.InvokeAsync<bool>(
					"RoomValidator",
					"ValidateRoomDescription",
					new KernelArguments
					{
						["description"] = rendered,
						["minSentences"] = ctx["minSentences"],
						["maxSentences"] = ctx["maxSentences"],
						["isFocused"] = isSpecific,
						["focus"] = focus,
					});

				if (isValid)
				{
					_logger.LogInformation("Validation passed on attempt {Attempt}", attempt);
				}

				return (Result: rendered, IsValid: isValid);
			},
			context);

		_logger.LogInformation("Total render attempts: {Attempts}", attempt);
		return result.Result;
	}

	#endregion
}
