using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using OllamaSharp.Models.Chat;
using OllamaSharp.Tools;

namespace AINPC.Tools
{
	/// <summary>
	/// Base class for all AINPC tool definitions.
	/// Provides argument conversion, dictionary parsing,
	/// and template method execution for tool invocation.
	/// </summary>
	public abstract class BaseOllamaTool : Tool, IInvokableTool
	{
		protected BaseOllamaTool(string name, string description)
		{
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

		/// <summary>
		/// Subclasses must implement this. All args are already converted to
		/// their proper CLR types before reaching this method.
		/// </summary>
		protected abstract object? InvokeInternal(IDictionary<string, object?> args);

		/// <summary>
		/// Main method called by OllamaSharp when the LLM invokes a tool.
		/// Performs type coercion, enum parsing, and provides cleaned arguments.
		/// </summary>
		public object? InvokeMethod(IDictionary<string, object?>? rawArgs)
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

			return InvokeInternal(converted);
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
	}
}
