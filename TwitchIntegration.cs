using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;

namespace TwitchPredictionTest
{
    public class TwitchIntegration
    {
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly string _oauthToken;
        private readonly string _broadcasterId;
        private readonly string _baseUrl = "https://api.twitch.tv/helix";

        private JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

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

                var json = JsonSerializer.Serialize(data, _jsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine($"Create prediction response: {responseContent}"); // Debug line

                if (response.IsSuccessStatusCode)
                {
                    var predictionResponse = JsonSerializer.Deserialize<PredictionResponse>(responseContent, _jsonOptions);
                    if (predictionResponse?.Data != null && predictionResponse.Data.Count > 0)
                    {
                        Console.WriteLine("Prediction created successfully.");
                        return predictionResponse.Data[0].Id;
                    }
                }
                
                Console.WriteLine($"Failed to create prediction: {response.StatusCode}, {responseContent}");
                return null;
            }
        }

        public async Task<List<Prediction>> GetActivePredictionsAsync()
        {
            using (var client = CreateHttpClient())
            {
                var url = $"{_baseUrl}/predictions?broadcaster_id={_broadcasterId}";
                var response = await client.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"Failed to fetch predictions: {response.StatusCode}, {await response.Content.ReadAsStringAsync()}");
                    return new List<Prediction>();
                }

                try
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw prediction response: {responseJson}"); // Debug line
                    var predictionResponse = JsonSerializer.Deserialize<PredictionResponse>(responseJson, _jsonOptions);
                    
                    if (predictionResponse?.Data == null)
                    {
                        Console.WriteLine("No predictions data returned");
                        return new List<Prediction>();
                    }

                    var activePredictions = predictionResponse.Data
                        .Where(p => p.Status == "ACTIVE" || p.Status == "LOCKED")
                        .ToList();

                    if (activePredictions.Count == 0)
                    {
                        Console.WriteLine("No active predictions found.");
                    }
                    else
                    {
                        Console.WriteLine($"Found {activePredictions.Count} active predictions");
                    }

                    return activePredictions;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error parsing prediction data: {ex.Message}");
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

        public class PredictionResponse
        {
            public List<Prediction> Data { get; set; }
        }

        public class Prediction
        {
            public string Id { get; set; }
            public string BroadcasterId { get; set; }
            public string BroadcasterName { get; set; }
            public string BroadcasterLogin { get; set; }
            public string Title { get; set; }
            public string WinningOutcomeId { get; set; }
            public string Status { get; set; }
            public int Duration { get; set; }
            public DateTime CreatedAt { get; set; }
            public List<Outcome> Outcomes { get; set; }
        }

        public class Outcome
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public int Users { get; set; }
            public int ChannelPoints { get; set; }
            public string Color { get; set; }
        }
    }
}