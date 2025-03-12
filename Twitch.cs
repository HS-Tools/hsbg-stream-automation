using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

public class TwitchIntegration
{
    private readonly string _clientId;
    private readonly string _clientSecret;
    private readonly string _oauthToken;
    private readonly string _broadcasterId;
    private readonly string _baseUrl = "https://api.twitch.tv/helix";

    public TwitchIntegration(string clientId, string clientSecret, string oauthToken, string broadcasterId)
    {
        _clientId = clientId;
        _clientSecret = clientSecret;
        _oauthToken = oauthToken;
        _broadcasterId = broadcasterId;
    }

    private HttpClient CreateHttpClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Add("Client-ID", _clientId);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthToken);
        return client;
    }

    public async Task<string> CreatePredictionAsync(string title, List<string> outcomes, int duration)
    {
        using (var client = CreateHttpClient())
        {
            var url = $"{_baseUrl}/predictions";
            var data = new
            {
                broadcaster_id = _broadcasterId,
                title = title,
                outcomes = outcomes.ConvertAll(outcome => new { title = outcome }),
                prediction_window = duration
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var predictionResponse = JsonSerializer.Deserialize<PredictionResponse>(responseJson);
                Console.WriteLine("Prediction created successfully.");
                return predictionResponse.Data[0].Id;
            }
            else
            {
                Console.WriteLine($"Failed to create prediction: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                return null;
            }
        }
    }

    public async Task<List<Prediction>> GetActivePredictionsAsync()
    {
        using (var client = CreateHttpClient())
        {
            var url = $"{_baseUrl}/predictions?broadcaster_id={_broadcasterId}";
            var response = await client.GetAsync(url);

            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync();
                var predictionResponse = JsonSerializer.Deserialize<PredictionResponse>(responseJson);
                var activePredictions = predictionResponse.Data.FindAll(p => p.Status == "ACTIVE" || p.Status == "LOCKED");

                if (activePredictions.Count == 0)
                {
                    Console.WriteLine("No active predictions found.");
                }

                return activePredictions;
            }
            else
            {
                Console.WriteLine($"Failed to fetch predictions: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                return new List<Prediction>();
            }
        }
    }

    public async Task EndPredictionAsync(string predictionId, string winningOutcomeId = null)
    {
        using (var client = CreateHttpClient())
        {
            var url = $"{_baseUrl}/predictions";
            var data = new
            {
                broadcaster_id = _broadcasterId,
                id = predictionId,
                status = winningOutcomeId == null ? "CANCELED" : "RESOLVED",
                winning_outcome_id = winningOutcomeId
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PatchAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Prediction updated successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to update prediction: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }
        }
    }

    public async Task RunAdAsync(int length)
    {
        using (var client = CreateHttpClient())
        {
            var url = $"{_baseUrl}/channels/commercial";
            var data = new
            {
                broadcaster_id = _broadcasterId,
                length = length
            };

            var json = JsonSerializer.Serialize(data);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await client.PostAsync(url, content);
            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine("Ad started successfully.");
            }
            else
            {
                Console.WriteLine($"Failed to run ad: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
            }
        }
    }

    private class PredictionResponse
    {
        public List<Prediction> Data { get; set; }
    }

    private class Prediction
    {
        public string Id { get; set; }
        public string Status { get; set; }
        public List<Outcome> Outcomes { get; set; }
    }

    private class Outcome
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }
}