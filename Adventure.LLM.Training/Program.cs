// NOTE: This is a full reimplementation of your example using a *real* 1K-parameter
// decoder-style transformer (single-head self-attention, causal mask).
// It preserves your PythonFactory / Python.NET lifecycle and replaces only the model.

using Adventure.LLM.Training.EnvironmentManagers;
using Python.Runtime;

namespace Adventure.LLM.Training;

internal sealed class NanoTransformerWrapper : IDisposable
{
	private readonly IPythonEnvironmentManager _envManager;
	private PyObject? _testFn;
	private bool _initialized;
	private bool _disposed;

	public NanoTransformerWrapper()
	{
		_envManager = new PythonFactory().GetEnvironmentManager("Adventure");
	}

	public async Task InitializeAsync()
	{
		if (_initialized) return;

		if (!await _envManager.SetupEnvironmentAsync())
			throw new InvalidOperationException("Failed to setup Python environment");

		var packageManager = new PythonFactory()
			.GetPackageManager(_envManager.GetPythonHome()!);

		if (!await packageManager.IsPackageInstalledAsync("torch"))
			await packageManager.InstallPackageAsync("torch");

		_envManager.Initialize();

		using (Py.GIL())
		using (var scope = Py.CreateScope())
		{
			scope.Exec(PYTHON_SCRIPT);
			_testFn = scope.Get("test");
		}

		_initialized = true;
	}

	public string Predict(double[] heats)
	{
		if (!_initialized)
			throw new InvalidOperationException("Call InitializeAsync first");

		if (heats.Length != 3)
			throw new ArgumentException("Expected exactly 3 heat values");

		using (Py.GIL())
		{
			using var heatsTuple = new PyTuple(
				heats.Select(h => new PyFloat(h)).ToArray()
			);

			using var args = new PyTuple(new PyObject[] { heatsTuple });

			using var result = _testFn!.Invoke(args);
			return result.ToString()!;
		}
	}

	public void Dispose()
	{
		if (_disposed) return;

		if (PythonEngine.IsInitialized)
		{
			using (Py.GIL())
			{
				_testFn?.Dispose();
			}
		}

		_envManager.Shutdown();
		_disposed = true;
	}

	// ---------------------------------------------------------------------
	// Python: 1K-parameter causal decoder transformer
	// ---------------------------------------------------------------------
	private const string PYTHON_SCRIPT = @"
import torch
import torch.nn as nn
import torch.optim as optim
import math
import random

TOKENS = [
    'H-1.0', 'H-0.5', 'H0.0', 'H0.5', 'H1.0',
    'LEFT', 'STAY', 'RIGHT'
]

stoi = {t: i for i, t in enumerate(TOKENS)}
itos = {i: t for t, i in stoi.items()}
VOCAB = len(TOKENS)

# -------------------------------
# Tiny decoder transformer
# -------------------------------
class DecoderBlock(nn.Module):
    def __init__(self, d_model):
        super().__init__()
        self.qkv = nn.Linear(d_model, d_model * 3, bias=False)
        self.proj = nn.Linear(d_model, d_model, bias=False)
        self.ff = nn.Sequential(
            nn.Linear(d_model, d_model * 2),
            nn.ReLU(),
            nn.Linear(d_model * 2, d_model)
        )
        self.ln1 = nn.LayerNorm(d_model)
        self.ln2 = nn.LayerNorm(d_model)

    def forward(self, x):
        B, T, C = x.shape
        q, k, v = self.qkv(x).chunk(3, dim=-1)

        att = (q @ k.transpose(-2, -1)) / math.sqrt(C)
        mask = torch.tril(torch.ones(T, T))
        att = att.masked_fill(mask == 0, -1e9)
        att = att.softmax(dim=-1)

        x = x + self.proj(att @ v)
        x = self.ln1(x)
        x = x + self.ff(x)
        x = self.ln2(x)
        return x

class NanoDecoder(nn.Module):
    def __init__(self):
        super().__init__()
        d_model = 16
        self.embed = nn.Embedding(VOCAB, d_model)
        self.block = DecoderBlock(d_model)
        self.head = nn.Linear(d_model, 3)

    def forward(self, x):
        x = self.embed(x)
        x = self.block(x)
        return self.head(x[:, -1])

# -------------------------------
# Training data
# -------------------------------
def heat_to_token(h):
    if h < -0.75: return 'H-1.0'
    if h < -0.25: return 'H-0.5'
    if h < 0.25:  return 'H0.0'
    if h < 0.75:  return 'H0.5'
    return 'H1.0'

def generate_example():
    heats = [random.uniform(-1, 1) for _ in range(3)]
    tokens = [stoi[heat_to_token(h)] for h in heats]
    best = heats.index(max(heats))
    return tokens, best

# -------------------------------
# Train
# -------------------------------
model = NanoDecoder()
params = sum(p.numel() for p in model.parameters())
print('Parameters:', params)

opt = optim.Adam(model.parameters(), lr=3e-3)
loss_fn = nn.CrossEntropyLoss()

for step in range(3000):
    xb, yb = [], []
    for _ in range(32):
        x, y = generate_example()
        xb.append(x)
        yb.append(y)

    x = torch.tensor(xb)
    y = torch.tensor(yb)

    logits = model(x)
    loss = loss_fn(logits, y)

    opt.zero_grad()
    loss.backward()
    opt.step()

# -------------------------------
# Inference
# -------------------------------
def test(heats):
    tokens = [stoi[heat_to_token(h)] for h in heats]
    x = torch.tensor([tokens])
    logits = model(x)
    return ['LEFT', 'STAY', 'RIGHT'][torch.argmax(logits).item()]
";
}

public class TrainingResult
{
	public int ParameterCount { get; set; }
	public List<float> Losses { get; set; } = new();
	public float FinalLoss { get; set; }
}

internal static class Program
{
	static async Task Main(string[] args)
	{
		Console.WriteLine("Nano 1K Decoder Transformer Demo");
		Console.WriteLine("================================\n");

		try
		{
			using var model = new NanoTransformerWrapper();

			Console.WriteLine("Initializing Python environment and training transformer...");
			await model.InitializeAsync();
			Console.WriteLine("Model ready.\n");

			// --------------------------------------------------
			// Fixed test cases
			// --------------------------------------------------
			Console.WriteLine("Test Cases");
			Console.WriteLine("----------");

			var testCases = new[]
			{
				new[] { -0.9,  0.1,  0.8 },   // RIGHT
				new[] {  0.9, -0.2, -0.5 },   // LEFT
				new[] { -0.1,  0.0,  0.1 },   // RIGHT
				new[] {  0.0,  1.0,  0.0 },   // STAY
				new[] { -1.0, -0.5, -0.8 }    // STAY
			};

			foreach (var heats in testCases)
			{
				var action = model.Predict(heats);
				Console.WriteLine(
					$"[{string.Join(", ", heats.Select(h => h.ToString("F2")))}] → {action}"
				);
			}

			// --------------------------------------------------
			// Interactive mode
			// --------------------------------------------------
			Console.WriteLine("\nInteractive Mode");
			Console.WriteLine("----------------");
			Console.WriteLine("Enter three heat values (-1.0 to 1.0), or 'quit' to exit.");

			while (true)
			{
				Console.Write("> ");
				var input = Console.ReadLine();

				if (string.IsNullOrWhiteSpace(input))
					continue;

				if (input.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
					break;

				try
				{
					var values = input
						.Split(' ', StringSplitOptions.RemoveEmptyEntries)
						.Select(double.Parse)
						.ToArray();

					if (values.Length != 3)
					{
						Console.WriteLine("Please enter exactly 3 values.");
						continue;
					}

					if (values.Any(v => v < -1.0 || v > 1.0))
					{
						Console.WriteLine("Values must be between -1.0 and 1.0.");
						continue;
					}

					var action = model.Predict(values);
					Console.WriteLine($"→ Recommended action: {action}");
				}
				catch (FormatException)
				{
					Console.WriteLine("Invalid input. Use numbers only.");
				}
				catch (Exception ex)
				{
					Console.WriteLine($"Error: {ex.Message}");
				}
			}

			Console.WriteLine("\nExiting.");
		}
		catch (Exception ex)
		{
			Console.WriteLine("Fatal error:");
			Console.WriteLine(ex.Message);
			Console.WriteLine(ex.StackTrace);
		}
	}
}
