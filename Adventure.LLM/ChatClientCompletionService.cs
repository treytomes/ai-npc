using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Services;
using System.Runtime.CompilerServices;

namespace Adventure.LLM;

public class ChatClientCompletionService : IChatCompletionService
{
	private readonly IChatClient _chatClient;
	private readonly string _modelId;
	private readonly Dictionary<string, object?> _attributes;

	public IReadOnlyDictionary<string, object?> Attributes => _attributes;

	public ChatClientCompletionService(
		IChatClient chatClient,
		string modelId,
		Dictionary<string, object?>? attributes = null)
	{
		_chatClient = chatClient ?? throw new ArgumentNullException(nameof(chatClient));
		_modelId = modelId;
		_attributes = attributes ?? new Dictionary<string, object?>
		{
			[AIServiceExtensions.ModelIdKey] = _modelId
		};
	}

	public async Task<IReadOnlyList<ChatMessageContent>> GetChatMessageContentsAsync(
		ChatHistory chatHistory,
		PromptExecutionSettings? executionSettings = null,
		Kernel? kernel = null,
		CancellationToken cancellationToken = default)
	{
		var messages = ConvertToChatMessages(chatHistory);
		var options = ConvertToChatOptions(executionSettings);

		var response = await _chatClient.GetResponseAsync(
			messages,
			options,
			cancellationToken);

		return new List<ChatMessageContent>
		{
			new ChatMessageContent(
				role: AuthorRole.Assistant,
				content: response.Text,
				modelId: _modelId,
				innerContent: response,
				metadata: ExtractMetadata(response))
		};
	}

	public async IAsyncEnumerable<StreamingChatMessageContent> GetStreamingChatMessageContentsAsync(
		ChatHistory chatHistory,
		PromptExecutionSettings? executionSettings = null,
		Kernel? kernel = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var messages = ConvertToChatMessages(chatHistory);
		var options = ConvertToChatOptions(executionSettings);

		await foreach (var update in _chatClient.GetStreamingResponseAsync(
			messages,
			options,
			cancellationToken))
		{
			if (!string.IsNullOrEmpty(update.Text))
			{
				yield return new StreamingChatMessageContent(
					role: AuthorRole.Assistant,
					content: update.Text,
					modelId: _modelId,
					innerContent: update,
					metadata: ExtractMetadata(update));
			}
		}
	}

	private List<ChatMessage> ConvertToChatMessages(ChatHistory chatHistory)
	{
		var messages = new List<ChatMessage>();

		foreach (var message in chatHistory)
		{
			var role = message.Role.Label switch
			{
				"system" => ChatRole.System,
				"user" => ChatRole.User,
				"assistant" => ChatRole.Assistant,
				"tool" => ChatRole.Tool,
				_ => ChatRole.User
			};

			// Handle multi-modal content
			if (message.Items is { Count: > 0 })
			{
				var contents = new List<AIContent>();

				foreach (var item in message.Items)
				{
					switch (item)
					{
						case Microsoft.SemanticKernel.TextContent textContent:
							contents.Add(new Microsoft.Extensions.AI.TextContent(textContent.Text));
							break;
						case ImageContent imageContent:
							contents.Add(new DataContent(
								imageContent.Data?.ToArray() ?? Array.Empty<byte>(),
								imageContent.MimeType ?? "text/plain"));
							break;
							// Add other content types as needed.
					}
				}

				messages.Add(new ChatMessage(role, contents));
			}
			else
			{
				// Simple text message
				messages.Add(new ChatMessage(role, message.Content));
			}
		}

		return messages;
	}

	private ChatOptions? ConvertToChatOptions(PromptExecutionSettings? settings)
	{
		if (settings == null)
			return null;

		var options = new ChatOptions
		{
			ModelId = settings.ModelId ?? _modelId,
			AdditionalProperties = new(),
		};

		// Map common settings
		if (settings.ExtensionData != null)
		{
			if (settings.ExtensionData.TryGetValue("temperature", out var temp))
				options.Temperature = Convert.ToSingle(temp);

			if (settings.ExtensionData.TryGetValue("max_tokens", out var maxTokens))
				options.MaxOutputTokens = Convert.ToInt32(maxTokens);

			if (settings.ExtensionData.TryGetValue("top_p", out var topP))
				options.TopP = Convert.ToSingle(topP);

			if (settings.ExtensionData.TryGetValue("frequency_penalty", out var freqPenalty))
				options.FrequencyPenalty = Convert.ToSingle(freqPenalty);

			if (settings.ExtensionData.TryGetValue("presence_penalty", out var presPenalty))
				options.PresencePenalty = Convert.ToSingle(presPenalty);

			if (settings.ExtensionData.TryGetValue("stop_sequences", out var stopSeq) && stopSeq is IList<string> stops)
				options.StopSequences = stops;

			// Copy any additional settings
			foreach (var kvp in settings.ExtensionData)
			{
				if (!IsCommonSetting(kvp.Key))
				{
					options.AdditionalProperties[kvp.Key] = kvp.Value;
				}
			}
		}

		return options;
	}

	private static bool IsCommonSetting(string key)
	{
		return key switch
		{
			"temperature" or "max_tokens" or "top_p" or
			"frequency_penalty" or "presence_penalty" or
			"stop_sequences" => true,
			_ => false
		};
	}

	private Dictionary<string, object?>? ExtractMetadata(ChatResponse response)
	{
		var metadata = new Dictionary<string, object?>();

		if (response.ConversationId != null)
			metadata["conversation_id"] = response.ConversationId;

		if (response.ResponseId != null)
			metadata["response_id"] = response.ResponseId;

		if (response.FinishReason != null)
			metadata["finish_reason"] = response.FinishReason.ToString();

		if (response.CreatedAt != null)
			metadata["created_at"] = response.CreatedAt.ToString();

		if (response.AdditionalProperties != null)
			foreach (var kv in response.AdditionalProperties)
			{
				metadata[kv.Key] = kv.Value;
			}

		return metadata.Count > 0 ? metadata : null;
	}

	private Dictionary<string, object?>? ExtractMetadata(ChatResponseUpdate response)
	{
		var metadata = new Dictionary<string, object?>();

		if (response.ConversationId != null)
			metadata["conversation_id"] = response.ConversationId;

		if (response.MessageId != null)
			metadata["message_id"] = response.MessageId;

		if (response.ResponseId != null)
			metadata["response_id"] = response.ResponseId;

		if (response.FinishReason != null)
			metadata["finish_reason"] = response.FinishReason.ToString();

		if (response.AuthorName != null)
			metadata["author_name"] = response.AuthorName.ToString();

		if (response.CreatedAt != null)
			metadata["created_at"] = response.CreatedAt.ToString();

		if (response.AdditionalProperties != null)
			foreach (var kv in response.AdditionalProperties)
			{
				metadata[kv.Key] = kv.Value;
			}

		return metadata.Count > 0 ? metadata : null;
	}
}