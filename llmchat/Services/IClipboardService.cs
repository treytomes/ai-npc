using System.Threading.Tasks;

namespace llmchat.Services;

public interface IClipboardService
{
	Task SetTextAsync(string text);
}
