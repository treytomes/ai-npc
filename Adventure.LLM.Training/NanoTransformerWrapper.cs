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
	private PyObject? _model;
	private PyObject? _torch;
	private PyObject? _nn;
	private PyObject? _math;
	private PyObject? _nanoDecoderClass;
	private Dictionary<string, int> _stoi = new();
	private Dictionary<int, string> _itos = new();
	private bool _initialized;
	private bool _disposed;
	private Random _random = new Random();

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

		// Build the string-to-index and index-to-string mappings in C#
		for (int i = 0; i < TOKENS.Length; i++)
		{
			_stoi[TOKENS[i]] = i;
			_itos[i] = TOKENS[i];
		}

		using (Py.GIL())
		using (var scope = Py.CreateScope())
		{
			// Import required modules
			_torch = Py.Import("torch");
			_nn = _torch.GetAttr("nn");
			_math = Py.Import("math");

			// Set modules in scope
			scope.Set("torch", _torch);
			scope.Set("nn", _nn);
			scope.Set("math", _math);

			// Create Python dictionaries from C# dictionaries
			using var pyStoiDict = new PyDict();
			foreach (var kvp in _stoi)
			{
				using var key = new PyString(kvp.Key);
				using var value = new PyInt(kvp.Value);
				pyStoiDict[key] = value;
			}

			using var pyItosDict = new PyDict();
			foreach (var kvp in _itos)
			{
				using var key = new PyInt(kvp.Key);
				using var value = new PyString(kvp.Value);
				pyItosDict[key] = value;
			}

			// Set all variables in Python scope
			scope.Set("TOKENS", TOKENS);
			scope.Set("VOCAB", VOCAB);
			scope.Set("D_MODEL", D_MODEL);
			scope.Set("stoi", pyStoiDict);
			scope.Set("itos", pyItosDict);

			// Execute model definition
			scope.Exec(GetModelArchitecture());

			// Get reference to model class
			_nanoDecoderClass = scope.Get("NanoDecoder");

			// Create model instance
			using var dModelArg = new PyInt(D_MODEL);
			_model = _nanoDecoderClass.Invoke(dModelArg);

			// Calculate and print parameter count
			int paramCount = CalculateParameterCount();
			Console.WriteLine($"Parameters: {paramCount}");
		}

		_initialized = true;
	}

	private int CalculateParameterCount()
	{
		using (Py.GIL())
		{
			using var parameters = _model!.GetAttr("parameters");
			using var paramsGenerator = parameters.Invoke();

			int totalParams = 0;

			// Iterate through all parameters
			using var iter = PyIter.GetIter(paramsGenerator);
			while (iter.MoveNext())
			{
				var param = iter.Current;
				using (param)
				{
					using var numel = param.GetAttr("numel");
					using var numelResult = numel.Invoke();
					totalParams += numelResult.As<int>();
				}
			}

			return totalParams;
		}
	}

	private (int[] tokens, int label) GenerateExample()
	{
		// Generate random heat values
		var heats = new double[3];
		for (int i = 0; i < 3; i++)
		{
			heats[i] = _random.NextDouble() * 2 - 1; // Range: -1.0 to 1.0
		}

		// Convert to tokens
		var tokens = new int[3];
		for (int i = 0; i < 3; i++)
		{
			string token = HeatToToken(heats[i]);
			tokens[i] = _stoi[token];
		}

		// Find best action (highest heat)
		int best = 0;
		double maxHeat = heats[0];
		for (int i = 1; i < 3; i++)
		{
			if (heats[i] > maxHeat)
			{
				maxHeat = heats[i];
				best = i;
			}
		}

		return (tokens, best);
	}

	public void Train(int steps = 3000, int batchSize = 32)
	{
		using (Py.GIL())
		{
			// Get optimizer and loss function
			using var optimModule = _torch!.GetAttr("optim");
			using var adamClass = optimModule.GetAttr("Adam");
			using var parameters = _model!.GetAttr("parameters");
			using var paramsResult = parameters.Invoke();

			// Create optimizer with learning rate
			using var lr = new PyFloat(0.003);
			using var kwargs = new PyDict();
			kwargs["lr"] = lr;
			using var optimizer = adamClass.Invoke(new PyTuple([paramsResult]), kwargs);

			// Get loss function
			using var crossEntropyLoss = _nn!.GetAttr("CrossEntropyLoss");
			using var lossFn = crossEntropyLoss.Invoke();

			// Training loop
			for (int step = 0; step < steps; step++)
			{
				// Generate batch
				var xBatch = new List<int[]>();
				var yBatch = new List<int>();

				for (int i = 0; i < batchSize; i++)
				{
					var (tokens, label) = GenerateExample();
					xBatch.Add(tokens);
					yBatch.Add(label);
				}

				// Convert to Python tensors
				using var xList = new PyList();
				foreach (var tokens in xBatch)
				{
					using var tokenList = new PyList();
					foreach (var token in tokens)
					{
						tokenList.Append(new PyInt(token));
					}
					xList.Append(tokenList);
				}

				using var yList = new PyList();
				foreach (var label in yBatch)
				{
					yList.Append(new PyInt(label));
				}

				using var tensorFunc = _torch.GetAttr("tensor");
				using var x = tensorFunc.Invoke(xList);
				using var y = tensorFunc.Invoke(yList);

				// Forward pass
				using var logits = _model.Invoke(x);
				using var loss = lossFn.Invoke(new PyTuple([logits, y]));

				// Backward pass
				using var zeroGrad = optimizer.GetAttr("zero_grad");
				zeroGrad.Invoke();

				using var backward = loss.GetAttr("backward");
				backward.Invoke();

				using var stepMethod = optimizer.GetAttr("step");
				stepMethod.Invoke();

				// Optional: Print progress every 500 steps
				if ((step + 1) % 500 == 0)
				{
					using var item = loss.GetAttr("item");
					using var lossValue = item.Invoke();
					Console.WriteLine($"Step {step + 1}/{steps}, Loss: {lossValue.As<float>():F4}");
				}
			}

			Console.WriteLine("Training complete!");
		}
	}

	public string Predict(double[] heats)
	{
		if (!_initialized)
			throw new InvalidOperationException("Call InitializeAsync first");

		if (heats.Length != 3)
			throw new ArgumentException("Expected exactly 3 heat values");

		using (Py.GIL())
		{
			// Convert heats to tokens in C#
			var tokenIndices = new int[3];
			for (int i = 0; i < 3; i++)
			{
				string token = HeatToToken(heats[i]);
				tokenIndices[i] = _stoi[token];
			}

			// Create Python tensor from token indices
			using var pyList = new PyList();
			using var innerList = new PyList();
			foreach (var idx in tokenIndices)
			{
				innerList.Append(new PyInt(idx));
			}
			pyList.Append(innerList);

			// Convert to tensor and run through model
			using var tensorFunc = _torch!.GetAttr("tensor");
			using var x = tensorFunc.Invoke(pyList);

			// Get model prediction
			using var logits = _model!.Invoke(x);

			// Get argmax
			using var argmaxFunc = _torch.GetAttr("argmax");
			using var prediction = argmaxFunc.Invoke(logits);
			using var itemFunc = prediction.GetAttr("item");
			using var result = itemFunc.Invoke();

			int actionIndex = result.As<int>();
			return new[] { "LEFT", "STAY", "RIGHT" }[actionIndex];
		}
	}

	private static string HeatToToken(double heat)
	{
		return heat switch
		{
			< -0.75 => "H-1.0",
			< -0.25 => "H-0.5",
			< 0.25 => "H0.0",
			< 0.75 => "H0.5",
			_ => "H1.0"
		};
	}

	public void Dispose()
	{
		if (_disposed) return;

		if (PythonEngine.IsInitialized)
		{
			using (Py.GIL())
			{
				_model?.Dispose();
				_torch?.Dispose();
				_nn?.Dispose();
				_math?.Dispose();
				_nanoDecoderClass?.Dispose();
			}
		}

		_envManager.Shutdown();
		_disposed = true;
	}

	// ---------------------------------------------------------------------
	// Model Architecture (pure Python class definitions)
	// ---------------------------------------------------------------------
	private static string GetModelArchitecture() => @"
# Tiny decoder transformer
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
";
}