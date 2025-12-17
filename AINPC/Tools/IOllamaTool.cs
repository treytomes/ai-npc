using OllamaSharp.Models.Chat;
using OllamaSharp.Tools;

namespace AINPC.Tools;

/// <summary>
/// Represents a callable tool that can be exposed to an Ollama-backed language model.
/// 
/// Tools are used to perform concrete actions or retrieve authoritative data
/// that the model should not hallucinate.
/// </summary>
public interface IOllamaTool : IAsyncInvokableTool
{
	/// <summary>
	/// A high-level intent string describing *why* this tool exists.
	/// 
	/// This is not an enum on purpose. It may be matched against:
	/// - prompt instructions
	/// - scripted logic
	/// - classifier outputs
	/// 
	/// Examples:
	/// - "shop.inventory.list"
	/// - "shop.transaction.purchase"
	/// - "weather.query"
	/// </summary>
	string Intent { get; }

	Function? Function { get; set; }
}