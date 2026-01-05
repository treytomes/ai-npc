using Adventure.LLM.Training.EnvironmentManagers;
using Python.Runtime;

namespace Adventure.LLM.Training;

internal sealed class NanoTransformerWrapper : IDisposable
{
	private static readonly string[] TOKENS =
	{
		"PAD",
		"HOT_LEFT",
		"HOT_RIGHT",
		"NO_HEAT",
		"ACTION",
		"LEFT",
		"RIGHT"
	};

	private const int PAD = 0;
	private const int VOCAB = 7;
	private const int D_MODEL = 12;
	private const int MAX_SEQ = 6;

	private readonly IPythonEnvironmentManager _envManager;
	private PyObject? _model;
	private PyObject? _torch;
	private PyObject? _nn;
	private PyObject? _math;
	private PyObject? _nanoDecoderClass;

	private readonly Dictionary<string, int> _stoi = new();
	private readonly Dictionary<int, string> _itos = new();
	private readonly Random _random = new();

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
			throw new InvalidOperationException("Python environment setup failed");

		var pkg = new PythonFactory()
			.GetPackageManager(_envManager.GetPythonHome()!);

		if (!await pkg.IsPackageInstalledAsync("torch"))
			await pkg.InstallPackageAsync("torch");

		_envManager.Initialize();

		for (int i = 0; i < TOKENS.Length; i++)
		{
			_stoi[TOKENS[i]] = i;
			_itos[i] = TOKENS[i];
		}

		using (Py.GIL())
		using (var scope = Py.CreateScope())
		{
			_torch = Py.Import("torch");
			_nn = _torch.GetAttr("nn");
			_math = Py.Import("math");

			scope.Set("torch", _torch);
			scope.Set("nn", _nn);
			scope.Set("math", _math);
			scope.Set("VOCAB", VOCAB);
			scope.Set("D_MODEL", D_MODEL);
			scope.Set("PAD", PAD);

			scope.Exec(GetModelCode());

			_nanoDecoderClass = scope.Get("NanoDecoder");
			_model = _nanoDecoderClass.Invoke(new PyInt(D_MODEL));

			Console.WriteLine($"Parameters: {CountParameters()}");
		}

		_initialized = true;
	}

	private int CountParameters()
	{
		using (Py.GIL())
		{
			int total = 0;
			using var ps = _model!.GetAttr("parameters").Invoke();
			using var it = PyIter.GetIter(ps);
			while (it.MoveNext())
			{
				using var p = it.Current;
				total += p.GetAttr("numel").Invoke().As<int>();
			}
			return total;
		}
	}

	// -------------------------------------------------------
	// DATA GENERATION
	// -------------------------------------------------------

	private (int[] seq, int label) GenerateDelayedExample()
	{
		bool left = _random.Next(2) == 0;
		int delay = _random.Next(1, 4);

		var tokens = new List<int>
		{
			_stoi[left ? "HOT_LEFT" : "HOT_RIGHT"]
		};

		for (int i = 0; i < delay; i++)
			tokens.Add(_stoi["NO_HEAT"]);

		tokens.Add(_stoi["ACTION"]);

		while (tokens.Count < MAX_SEQ)
			tokens.Add(PAD);

		int label = left ? 0 : 1;
		return (tokens.ToArray(), label);
	}

	// -------------------------------------------------------
	// TRAINING
	// -------------------------------------------------------

	public void Train(int steps = 4000, int batchSize = 32)
	{
		using (Py.GIL())
		{
			using var optim = _torch!.GetAttr("optim").GetAttr("Adam");
			using var parms = _model!.GetAttr("parameters").Invoke();

			using var opt = optim.Invoke(
				new PyTuple(new[] { parms }),
				new PyDict { ["lr"] = new PyFloat(3e-3) }
			);

			using var lossFn = _nn!.GetAttr("CrossEntropyLoss").Invoke();

			for (int step = 1; step <= steps; step++)
			{
				var xb = new PyList();
				var yb = new PyList();

				for (int i = 0; i < batchSize; i++)
				{
					var (seq, label) = GenerateDelayedExample();
					var row = new PyList();
					foreach (var t in seq)
						row.Append(new PyInt(t));

					xb.Append(row);
					yb.Append(new PyInt(label));
				}

				using var x = _torch.GetAttr("tensor").Invoke(xb);
				using var y = _torch.GetAttr("tensor").Invoke(yb);

				using var logits = _model.Invoke(x);
				using var loss = lossFn.Invoke(new PyTuple(new[] { logits, y }));

				opt.GetAttr("zero_grad").Invoke();
				loss.GetAttr("backward").Invoke();
				opt.GetAttr("step").Invoke();

				if (step % 500 == 0)
				{
					float l = loss.GetAttr("item").Invoke().As<float>();
					Console.WriteLine($"Step {step}: Loss = {l:F4}");
				}
			}
		}
	}

	// -------------------------------------------------------
	// INFERENCE
	// -------------------------------------------------------

	public string Predict(bool heatLeft, int delay)
	{
		using (Py.GIL())
		{
			var seq = new List<int>
			{
				_stoi[heatLeft ? "HOT_LEFT" : "HOT_RIGHT"]
			};

			for (int i = 0; i < delay; i++)
				seq.Add(_stoi["NO_HEAT"]);

			seq.Add(_stoi["ACTION"]);
			while (seq.Count < MAX_SEQ)
				seq.Add(PAD);

			var outer = new PyList();
			var inner = new PyList();
			foreach (var t in seq)
				inner.Append(new PyInt(t));
			outer.Append(inner);

			using var x = _torch!.GetAttr("tensor").Invoke(outer);
			using var logits = _model!.Invoke(x);
			using var pred = _torch.GetAttr("argmax").Invoke(logits);

			int idx = pred.GetAttr("item").Invoke().As<int>();
			return idx == 0 ? "LEFT" : "RIGHT";
		}
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

	// -------------------------------------------------------
	// PYTHON MODEL
	// -------------------------------------------------------

	private static string GetModelCode() => @"
class DecoderBlock(nn.Module):
    def __init__(self, d):
        super().__init__()
        self.qkv = nn.Linear(d, d * 3, bias=False)
        self.proj = nn.Linear(d, d, bias=False)
        self.ff = nn.Sequential(
            nn.Linear(d, d * 2),
            nn.ReLU(),
            nn.Linear(d * 2, d)
        )
        self.ln1 = nn.LayerNorm(d)
        self.ln2 = nn.LayerNorm(d)

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
        return self.ln2(x)

class NanoDecoder(nn.Module):
    def __init__(self, d):
        super().__init__()
        self.embed = nn.Embedding(VOCAB, d, padding_idx=PAD)
        self.block = DecoderBlock(d)
        self.head = nn.Linear(d, 2)

    def forward(self, x):
        x = self.embed(x)
        x = self.block(x)
        return self.head(x[:, -1])
";
}
