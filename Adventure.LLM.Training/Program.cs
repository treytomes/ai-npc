// NOTE: This is a full reimplementation of your example using a *real* 1K-parameter
// decoder-style transformer (single-head self-attention, causal mask).
// It preserves your PythonFactory / Python.NET lifecycle and replaces only the model.

using Adventure.LLM.Training.EnvironmentManagers;
using Python.Runtime;

namespace Adventure.LLM.Training;

internal sealed class NanoTransformerWrapper : IDisposable
{
	private static readonly string[] TOKENS = [
		"H-1.0", "H-0.5", "H0.0", "H0.5", "H1.0",
		"LEFT", "STAY", "RIGHT"
	];

	private static readonly int VOCAB = TOKENS.Length;

	private const int D_MODEL = 12;

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
			scope.Set("TOKENS", TOKENS);
			scope.Set("VOCAB", VOCAB);
			scope.Set("D_MODEL", D_MODEL);
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

stoi = {t: i for i, t in enumerate(TOKENS)}
itos = {i: t for t, i in stoi.items()}

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
    def __init__(self, d_model):
        super().__init__()
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
model = NanoDecoder(D_MODEL)
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
			// Automated Testing
			// --------------------------------------------------
			const int NUM_TEST_CASES = 100;
			var random = new Random(42); // Fixed seed for reproducibility
			int correctPredictions = 0;
			var results = new List<(double[] heats, string expected, string actual, bool correct)>();

			Console.WriteLine($"Running {NUM_TEST_CASES} test cases...\n");

			for (int i = 0; i < NUM_TEST_CASES; i++)
			{
				// Generate random heat values
				var heats = new double[3];
				for (int j = 0; j < 3; j++)
				{
					heats[j] = random.NextDouble() * 2 - 1; // Range: -1.0 to 1.0
				}

				// Calculate expected result (go to highest heat source)
				int maxIndex = 0;
				double maxHeat = heats[0];
				for (int j = 1; j < 3; j++)
				{
					if (heats[j] > maxHeat)
					{
						maxHeat = heats[j];
						maxIndex = j;
					}
				}

				string expected = maxIndex switch
				{
					0 => "LEFT",
					1 => "STAY",
					2 => "RIGHT",
					_ => throw new InvalidOperationException()
				};

				// Get model prediction
				string actual = model.Predict(heats);

				// Track results
				bool isCorrect = expected == actual;
				if (isCorrect) correctPredictions++;

				results.Add((heats, expected, actual, isCorrect));
			}

			// --------------------------------------------------
			// Display Results
			// --------------------------------------------------
			Console.WriteLine("Test Results");
			Console.WriteLine("------------");

			// Show first 10 detailed results
			Console.WriteLine("\nFirst 10 test cases:");
			foreach (var (heats, expected, actual, correct) in results.Take(10))
			{
				var status = correct ? "✓" : "✗";
				Console.WriteLine($"{status} [{string.Join(", ", heats.Select(h => h.ToString("F2").PadLeft(5)))}] " +
								$"Expected: {expected,-5} Actual: {actual,-5}");
			}

			// Show some incorrect predictions if any
			var incorrectResults = results.Where(r => !r.correct).ToList();
			if (incorrectResults.Any())
			{
				Console.WriteLine($"\nSample incorrect predictions (showing up to 5):");
				foreach (var (heats, expected, actual, _) in incorrectResults.Take(5))
				{
					Console.WriteLine($"✗ [{string.Join(", ", heats.Select(h => h.ToString("F2").PadLeft(5)))}] " +
									$"Expected: {expected,-5} Actual: {actual,-5}");
				}
			}

			// Calculate and display accuracy
			double accuracy = (double)correctPredictions / NUM_TEST_CASES * 100;
			double errorRate = 100 - accuracy;

			Console.WriteLine("\n" + new string('=', 50));
			Console.WriteLine("Summary Statistics");
			Console.WriteLine(new string('=', 50));
			Console.WriteLine($"Total test cases:    {NUM_TEST_CASES}");
			Console.WriteLine($"Correct predictions: {correctPredictions}");
			Console.WriteLine($"Incorrect predictions: {NUM_TEST_CASES - correctPredictions}");
			Console.WriteLine($"Accuracy:            {accuracy:F2}%");
			Console.WriteLine($"Error rate:          {errorRate:F2}%");

			// Analyze errors by position
			var errorsByPosition = results
				.Where(r => !r.correct)
				.GroupBy(r => Array.IndexOf(r.heats, r.heats.Max()))
				.Select(g => new { Position = g.Key, Count = g.Count() })
				.OrderBy(x => x.Position);

			if (errorsByPosition.Any())
			{
				Console.WriteLine("\nErrors by correct position:");
				foreach (var error in errorsByPosition)
				{
					string position = error.Position switch
					{
						0 => "LEFT",
						1 => "STAY",
						2 => "RIGHT",
						_ => "UNKNOWN"
					};
					Console.WriteLine($"  {position}: {error.Count} errors");
				}
			}

			Console.WriteLine("\nTest complete.");
		}
		catch (Exception ex)
		{
			Console.WriteLine("Fatal error:");
			Console.WriteLine(ex.Message);
			Console.WriteLine(ex.StackTrace);
		}
	}
}