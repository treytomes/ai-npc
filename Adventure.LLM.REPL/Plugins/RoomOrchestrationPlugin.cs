using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Adventure.LLM.REPL.Plugins;

internal sealed class RoomOrchestrationPlugin
{
	private readonly Kernel _kernel;
	private readonly ILogger<RoomOrchestrationPlugin> _logger;
	private readonly Dictionary<string, object> _config;

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
	}

	[KernelFunction("RenderValidatedRoom")]
	[Description("Renders a room with automatic validation and retry")]
	public async Task<string> RenderValidatedRoomAsync(
		[Description("Room YAML data")] string roomYaml,
		[Description("User input")] string userInput)
	{
		var result = string.Empty;
		var attempts = 0;
		var maxAttempts = _config!.GetValueOrDefault("maxAttempts", 2)!;
		var sentenceCount = _config!.GetValueOrDefault("sentenceCount", "3-5")!;
		var minSentences = _config!.GetValueOrDefault("minSentences", "3")!;
		var maxSentences = _config!.GetValueOrDefault("maxSentences", "5")!;

		do
		{
			attempts++;
			_logger.LogInformation("Rendering attempt {Attempt}/{MaxAttempts}", attempts, maxAttempts);

			// Render the room.
			var ct = CancellationToken.None;
			result = await _kernel.InvokeAsync<string>(
				"RoomRenderer",
				"RenderRoom",
				new()
				{
					["roomYaml"] = roomYaml,
					["userInput"] = userInput,
					["sentenceCount"] = sentenceCount
				}, ct) ?? string.Empty;

			// Validate the result.
			var isValid = await _kernel.InvokeAsync<bool>(
				"RoomValidator",
				"ValidateRoomDescription",
				new KernelArguments
				{
					["description"] = result,
					["minSentences"] = minSentences,
					["maxSentences"] = maxSentences
				});

			if (isValid)
			{
				_logger.LogInformation("Validation passed on attempt {Attempt}", attempts);
				break;
			}

			_logger.LogWarning("Validation failed on attempt {Attempt}", attempts);
		}
		while (attempts < maxAttempts);

		_logger.LogInformation("Total render attempts: {Attempts}", attempts);
		return result;
	}
}
