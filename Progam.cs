using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetEnv;
Env.Load();

class Program
{
    static async Task Main(string[] args)
    {
        var clientId = Environment.GetEnvironmentVariable("CLIENT_ID");
        var clientSecret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
        var oauthToken = Environment.GetEnvironmentVariable("OAUTH_TOKEN");
        var broadcasterId = Environment.GetEnvironmentVariable("BROADCASTER_ID");

        var twitch = new TwitchIntegration(clientId, clientSecret, oauthToken, broadcasterId);

        // Create a new prediction
        var predictionId = await twitch.CreatePredictionAsync("Will I win this game?", new List<string> { "Yes", "No" }, 120);
        if (predictionId != null)
        {
            Console.WriteLine($"Prediction created with ID: {predictionId}");

            // End the prediction after some time
            await Task.Delay(60000); // Wait for 60 seconds
            await twitch.EndPredictionAsync(predictionId, "Yes"); // Resolve with "Yes"
        }
    }
}