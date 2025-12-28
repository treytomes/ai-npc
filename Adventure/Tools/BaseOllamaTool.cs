using OllamaSharp.Models.Chat;

namespace Adventure.Tools;

/// <summary>
/// Base class for all Adventure tool definitions.
/// 
/// Wraps OllamaSharp's <see cref="Tool"/> and provides:
/// - a stable intent string for classification
/// - argument coercion and validation
/// - a template method for execution
/// </summary>
internal abstract class BaseOllamaTool : Tool, IOllamaTool
{
	#region Constructors

	/// <summary>
	/// Creates a new Ollama tool with a name, description, and intent.
	/// </summary>
	/// <param name="name">
	/// The stable, human-readable name of the tool as exposed to the LLM.
	/// </param>
	/// <param name="description">
	/// A short description explaining what the tool does.
	/// This is used by the LLM to decide relevance.
	/// </param>
	/// <param name="intent">
	/// A high-level intent string describing why this tool exists.
	/// Example: "shop.inventory.list"
	/// </param>
	protected BaseOllamaTool(string name, string description, string intent)
	{
		Intent = intent ?? throw new ArgumentNullException(nameof(intent));

		Function = new Function
		{
			Name = name,
			Description = description,
			Parameters = new Parameters
			{
				Properties = new Dictionary<string, Property>(),
				Required = Array.Empty<string>()
			}
		};
	}

	#endregion

	#region Properties

	/// <summary>
	/// A high-level intent string describing the purpose of this tool.
	/// 
	/// This is used by classifiers, scripts, or orchestration logic
	/// to decide when the tool should be considered.
	/// </summary>
	public string Intent { get; }

	#endregion

	#region Methods

	protected abstract Task<object?> InvokeInternalAsync(IDictionary<string, object?> args);

	/// <summary>
	/// Main method called by OllamaSharp when the LLM invokes a tool.
	/// Performs type coercion, enum parsing, and provides cleaned arguments.
	/// </summary>
	public async Task<object?> InvokeMethodAsync(IDictionary<string, object?>? rawArgs)
	{
		var args = rawArgs ?? new Dictionary<string, object?>();

		// Convert arguments according to schema
		var converted = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

		if (Function?.Parameters?.Properties != null)
		{
			foreach (var kvp in Function.Parameters.Properties)
			{
				string argName = kvp.Key;
				var property = kvp.Value;

				args.TryGetValue(argName, out var rawValue);

				converted[argName] = ConvertArgument(rawValue, property);
			}
		}

		return await InvokeInternalAsync(converted);
	}

	/// <summary>
	/// Converts an argument from object? (string, double, JSON token, etc)
	/// into the CLR type that the subclass expects.
	/// </summary>
	private object? ConvertArgument(object? rawValue, Property property)
	{
		if (rawValue == null) return null;

		string? type = property.Type?.ToLowerInvariant();

		// Handle enums (property.Enum != null)
		if (property.Enum != null && property.Enum.Count() > 0)
		{
			return ParseEnum(property.Enum, rawValue);
		}

		return type switch
		{
			"string" => rawValue.ToString(),
			"number" => Convert.ToDouble(rawValue),
			"integer" => Convert.ToInt32(rawValue),
			"boolean" => Convert.ToBoolean(rawValue),
			_ => rawValue // unknown types just pass through
		};
	}

	private object ParseEnum(IEnumerable<string> values, object rawValue)
	{
		var asString = rawValue.ToString() ?? "";

		// Match ignoring case,
		foreach (var v in values)
		{
			if (string.Equals(v, asString, StringComparison.OrdinalIgnoreCase))
				return v;
		}

		// Default to first allowed value if unknown,
		return values.First();
	}

	/// <summary>
	/// Helper for tool authors to define a parameter.
	/// </summary>
	protected void DefineParameter(string name, string type, string description, string[]? enumValues = null, bool required = false)
	{
		if (Function?.Parameters?.Properties == null) return;

		Function.Parameters.Properties[name] = new Property
		{
			Type = type,
			Description = description,
			Enum = enumValues
		};

		if (required)
		{
			var list = new List<string>(Function.Parameters.Required ?? throw new NullReferenceException("Required parameter collection is null."))
				{
					name
				};
			Function.Parameters.Required = list.ToArray();
		}
	}

	#endregion
}
