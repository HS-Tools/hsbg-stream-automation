using System;
using System.Collections.Generic;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        try
        {
            Console.WriteLine("Testing Twitch Prediction API...");
            
            // Initialize config and integration
            var config = new TwitchConfig();
            var twitch = new TwitchIntegration(config);

            // Test creating a prediction
            Console.WriteLine("Attempting to create a test prediction...");
            var title = "Test Prediction";
            var outcomes = new List<string> { "Outcome A", "Outcome B" };
            var duration = 120; // 2 minutes

            var predictionId = await twitch.CreatePrediction(title, outcomes, duration);

            if (predictionId != null)
            {
                Console.WriteLine($"Success! Prediction created with ID: {predictionId}");
            }
            else
            {
                Console.WriteLine("Failed to create prediction - no ID returned");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
        }

        Console.WriteLine("\nPress any key to exit...");
        Console.ReadKey();
    }
} 