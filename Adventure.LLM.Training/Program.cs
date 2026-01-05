using Adventure.LLM.Training;

internal static class Program
{
	static async Task Main()
	{
		Console.WriteLine("Delayed Heat Choice – Nano Transformer\n");

		using var model = new NanoTransformerWrapper();
		await model.InitializeAsync();

		model.Train(4000, 32);

		Console.WriteLine("\nEvaluation:\n");

		int correct = 0;
		const int tests = 100;

		for (int i = 0; i < tests; i++)
		{
			bool left = i % 2 == 0;
			int delay = i % 4;

			string pred = model.Predict(left, delay);
			string expected = left ? "LEFT" : "RIGHT";

			bool ok = pred == expected;
			if (ok) correct++;

			Console.WriteLine(
				$"Heat: {(left ? "LEFT " : "RIGHT")} Delay: {delay} → {pred} {(ok ? "✓" : "✗")}"
			);
		}

		Console.WriteLine($"\nAccuracy: {(double)correct / tests:P2}");
	}
}
