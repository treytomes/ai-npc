using Adventure.LLM.Training.EnvironmentManagers;
using Python.Runtime;

namespace Adventure.LLM.Training;

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

			// Train the model
			model.Train(6000, 32);

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