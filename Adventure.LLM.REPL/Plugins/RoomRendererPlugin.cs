using System.ComponentModel;
using System.Text;
using Adventure.LLM.REPL.Templating;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.PromptTemplates.Handlebars;
using Spectre.Console;
using Spectre.Console.Rendering;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Adventure.LLM.REPL.Plugins;

internal sealed class RoomRendererPlugin
{
	#region Fields

	private readonly string _promptRootPath;
	private readonly IChatCompletionService _chatService;
	private readonly Kernel _kernel;
	private readonly ChatHistory _persistentHistory;
	private readonly ILogger<RoomRendererPlugin> _logger;
	private readonly KernelFunction _renderFunction;

	#endregion

	#region Constructors

	public RoomRendererPlugin(
		string promptRootPath,
		Kernel kernel,
		ChatHistory persistentHistory,
		ILogger<RoomRendererPlugin> logger)
	{
		_promptRootPath = promptRootPath;
		_kernel = kernel;
		_chatService = kernel.GetRequiredService<IChatCompletionService>();
		_persistentHistory = persistentHistory;
		_logger = logger;

		// Load and create the render function from template
		_renderFunction = CreateRenderFunction();
	}

	#endregion

	#region Methods

	private KernelFunction CreateRenderFunction()
	{
		var template = LoadPromptTemplate(Path.Combine(_promptRootPath, "room_renderer.yaml"));

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
						["max_tokens"] = template.ExecutionSettings.MaxTokens,
						["stop"] = template.ExecutionSettings.StopSequences ?? new List<string>()
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

	[KernelFunction("RenderRoom")]
	[Description("Renders a room description from YAML data")]
	public async Task<string> RenderRoomAsync(
		[Description("Room YAML")] string roomYaml,
		[Description("User input")] string userInput,
		[Description("Number of sentences")] string sentenceCount = "3-5",
		[Description("Optional focus area")] string focus = ""
	)
	{
		// Stream the response with visual feedback
		var result = await StreamRenderAsync(roomYaml, userInput, sentenceCount, focus);

		// Update persistent history
		_persistentHistory.AddUserMessage(userInput);
		_persistentHistory.AddAssistantMessage(result);

		return result;
	}

	private async Task<string> StreamRenderAsync(string roomYaml, string userInput, string sentenceCount, string focus)
	{
		var sb = new StringBuilder();
		var layout = (IRenderable)new Rows();
		var sentenceBuffer = new StringBuilder();

		var arguments = new KernelArguments
		{
			["roomYaml"] = roomYaml,
			["userInput"] = userInput,
			["sentenceCount"] = sentenceCount,
			["focus"] = focus,
		};

		// Determine header based on focus
		var header = string.IsNullOrEmpty(focus)
			? "[green]Narrator[/]"
			: $"[green]Narrator[/] [grey](focusing on: {focus})[/]";

		await AnsiConsole.Live(layout).StartAsync(async ctx =>
		{
			// Use the streaming invoke method
			await foreach (var update in _kernel.InvokeStreamingAsync(_renderFunction, arguments))
			{
				var text = update.ToString();
				if (string.IsNullOrWhiteSpace(text))
					continue;

				sentenceBuffer.Append(text);
				sb.Append(sentenceBuffer);
				sentenceBuffer.Clear();

				layout = new Panel(sb.ToString())
					.Header(header)
					.Border(BoxBorder.Rounded)
					.BorderColor(Color.Green);

				ctx.UpdateTarget(layout);
			}
		});

		return sb.Append(sentenceBuffer).ToString().Trim();
	}

	#endregion
}
