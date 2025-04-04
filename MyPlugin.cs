using Hearthstone_Deck_Tracker.API;
using HSBG_Ads_Predictions_for_Twitch.Controls;
using HSBG_Ads_Predictions_for_Twitch.Properties;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Core = Hearthstone_Deck_Tracker.API.Core;
using Hearthstone_Deck_Tracker.Enums;
using HearthDb.Enums;
using System.Media;
using HSBG_Ads_Predictions_for_Twitch.Configuration;

namespace HSBG_Ads_Predictions_for_Twitch
{
    public class TwitchConfig
    {
        public string ClientId { get; private set; }
        public string OAuthToken { get; private set; }

        public TwitchConfig(string clientId, string oauthToken)
        {
            ClientId = clientId;
            OAuthToken = oauthToken;
        }
    }

    public class TwitchIntegration
    {
        private readonly string _clientId;
        private readonly string _oauthToken;
        private string _broadcasterId;
        private readonly string _baseUrl = "https://api.twitch.tv/helix";
        private string _currentPredictionId;
        private List<Outcome> _currentOutcomes;

        public TwitchIntegration(TwitchConfig config)
        {
            _clientId = config.ClientId;
            _oauthToken = config.OAuthToken;
        }

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Client-ID", _clientId);
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _oauthToken.TrimStart("Bearer ".ToCharArray()));
            return client;
        }

        public async Task InitializeAsync()
        {
            _broadcasterId = await GetBroadcasterIdAsync();
            if (string.IsNullOrEmpty(_broadcasterId))
            {
                throw new Exception("Failed to get broadcaster ID. Please check your Twitch credentials.");
            }
        }

        private async Task<string> GetBroadcasterIdAsync()
        {
            using (var client = CreateHttpClient())
            {
                var response = await client.GetAsync($"{_baseUrl}/users");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var userData = JsonConvert.DeserializeObject<UserResponse>(content);
                    return userData?.Data?.FirstOrDefault()?.Id;
                }
                throw new Exception($"Failed to get user data: {response.StatusCode}");
            }
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

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var predictionResponse = JsonConvert.DeserializeObject<PredictionResponse>(responseContent);
                    if (predictionResponse?.Data != null && predictionResponse.Data.Count > 0)
                    {
                        _currentPredictionId = predictionResponse.Data[0].Id;
                        _currentOutcomes = predictionResponse.Data[0].Outcomes;
                        return _currentPredictionId;
                    }
                }
                
                return null;
            }
        }

        public async Task EndPredictionAsync(bool isTop)
        {
            if (_currentPredictionId == null || _currentOutcomes == null) return;

            using (var client = CreateHttpClient())
            {
                var url = $"{_baseUrl}/predictions";
                var winningOutcome = _currentOutcomes.FirstOrDefault(o => 
                    (isTop && o.Title == "Yes") || (!isTop && o.Title == "No"));

                if (winningOutcome == null) return;

                var data = new
                {
                    broadcaster_id = _broadcasterId,
                    id = _currentPredictionId,
                    status = "RESOLVED",
                    winning_outcome_id = winningOutcome.Id
                };

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = content
                };
                await client.SendAsync(request);
            }
        }

        public async Task RunAdAsync(int durationSeconds)
        {
            using (var client = CreateHttpClient())
            {
                var url = $"{_baseUrl}/channels/commercial";
                var data = new
                {
                    broadcaster_id = _broadcasterId,
                    length = durationSeconds
                };

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                await client.PostAsync(url, content);
            }
        }

        public async Task CancelActivePredictionsAsync()
        {
            var activePredictions = await GetActivePredictionsAsync();
            foreach (var prediction in activePredictions)
            {
                await CancelPredictionAsync(prediction.Id);
            }
        }

        private async Task<List<Prediction>> GetActivePredictionsAsync()
        {
            using (var client = CreateHttpClient())
            {
                var url = $"{_baseUrl}/predictions?broadcaster_id={_broadcasterId}";
                var response = await client.GetAsync(url);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    var predictionResponse = JsonConvert.DeserializeObject<PredictionResponse>(responseContent);
                    return predictionResponse?.Data?.Where(p => p.Status == "ACTIVE" || p.Status == "LOCKED").ToList() 
                           ?? new List<Prediction>();
                }
                
                return new List<Prediction>();
            }
        }

        private async Task CancelPredictionAsync(string predictionId)
        {
            using (var client = CreateHttpClient())
            {
                var url = $"{_baseUrl}/predictions";
                var data = new
                {
                    broadcaster_id = _broadcasterId,
                    id = predictionId,
                    status = "CANCELED"
                };

                var json = JsonConvert.SerializeObject(data);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), url)
                {
                    Content = content
                };
                await client.SendAsync(request);
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

        private class UserResponse
        {
            public List<UserData> Data { get; set; }
        }

        private class UserData
        {
            public string Id { get; set; }
        }
    }

    public class HSBG_Ads_Predictions_for_Twitch : IDisposable
    {
        private TwitchConfig _twitchConfig;
        private TwitchIntegration _twitchIntegration;
        private bool _predictionStarted = false;
        private Random _random = new Random();
        private int _currentTargetPlace;

        public HSBG_Ads_Predictions_for_Twitch()
        {
            InitTwitch();
            GameEvents.OnGameStart.Add(OnGameStart);
            GameEvents.OnTurnStart.Add(OnTurnStart);
            GameEvents.OnGameEnd.Add(OnGameEnd);
        }

        private async void InitTwitch()
        {
            try
            {
                // Load settings from persistent storage first
                PersistentSettings.LoadSettings();
                
                if (string.IsNullOrEmpty(Settings.Default.ClientId) || string.IsNullOrEmpty(Settings.Default.AccessToken))
                {
                    Hearthstone_Deck_Tracker.Utility.Logging.Log.Info("Twitch credentials not configured. Please set them in the plugin settings.");
                    return;
                }

                _twitchConfig = new TwitchConfig(
                    Settings.Default.ClientId,
                    Settings.Default.AccessToken.TrimStart("Bearer ".ToCharArray())
                );
                
                _twitchIntegration = new TwitchIntegration(_twitchConfig);
                await _twitchIntegration.InitializeAsync();
                Hearthstone_Deck_Tracker.Utility.Logging.Log.Info("Twitch integration initialized successfully");
            }
            catch (Exception ex)
            {
                Hearthstone_Deck_Tracker.Utility.Logging.Log.Error($"Failed to initialize Twitch integration: {ex.Message}");
                if (ex.Message.Contains("Unauthorized"))
                {
                    Hearthstone_Deck_Tracker.Utility.Logging.Log.Error("Please check your Twitch credentials in the plugin settings.");
                }
            }
        }

        public void OnGameStart()
        {
            _predictionStarted = false;
            _currentTargetPlace = 0;
        }

        public async void OnTurnStart(ActivePlayer player)
        {
            if (Core.Game?.CurrentGameMode != GameMode.Battlegrounds) return;
            if (Core.Game?.Player?.Hero?.Card == null) return;
            if (_predictionStarted) return;

            try
            {
                _predictionStarted = true;

                if (!Settings.Default.AutoRunPredictions || 
                    Settings.Default.PredictionChoices == null || 
                    Settings.Default.PredictionChoices.Count == 0)
                {
                    return;
                }
                
                await _twitchIntegration.CancelActivePredictionsAsync();
                
                var heroName = Core.Game.Player.Hero.Card.LocalizedName ?? "Hero";
                
                int[] predictionChoices;
                try 
                {
                    predictionChoices = Settings.Default.PredictionChoices.Cast<string>()
                        .Select(s => int.Parse(s.Trim()))
                        .Where(n => n >= 1 && n <= 8)
                        .ToArray();

                    if (predictionChoices == null || predictionChoices.Length == 0)
                    {
                        return;
                    }
                }
                catch (Exception)
                {
                    return;
                }
                
                _currentTargetPlace = predictionChoices[_random.Next(predictionChoices.Length)];
                
                var title = $"Top {_currentTargetPlace} with {heroName}?";
                // Ensure title doesn't exceed 60 characters
                if (title.Length > 60)
                {
                    title = title.Substring(0, 57) + "...";
                }
                var outcomes = new List<string> { "Yes", "No" };
                await _twitchIntegration.CreatePredictionAsync(title, outcomes, 120);

                // Play sound if the setting is enabled
                if (Settings.Default.PlaySoundOnPrediction)
                {
                    try
                    {
                        var soundPath = System.IO.Path.Combine(
                            System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location),
                            "alert.wav"
                        );
                        using (var soundPlayer = new SoundPlayer(soundPath))
                        {
                            soundPlayer.Play();
                        }
                    }
                    catch (Exception ex)
                    {
                        Hearthstone_Deck_Tracker.Utility.Logging.Log.Error($"Failed to play sound: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                Hearthstone_Deck_Tracker.Utility.Logging.Log.Error($"Failed to start prediction: {ex.Message}");
            }
        }

        public async void OnGameEnd()
        {
            _predictionStarted = false;
            if (Core.Game.CurrentGameMode != GameMode.Battlegrounds) return;

            try
            {
                if (Settings.Default.AutoRunPredictions && _currentTargetPlace > 0)
                {
                    var playerEntity = Core.Game.Entities.Values.FirstOrDefault(x => x.IsPlayer);
                    var playerPlaceEntity = Core.Game.Entities.Values
                        .FirstOrDefault(e => e.GetTag(GameTag.PLAYER_ID) == playerEntity.GetTag(GameTag.PLAYER_ID)
                                        && e.HasTag(GameTag.PLAYER_LEADERBOARD_PLACE));

                    var placement = playerPlaceEntity?.GetTag(GameTag.PLAYER_LEADERBOARD_PLACE) ??
                                        playerEntity.GetTag(GameTag.PLAYER_LEADERBOARD_PLACE);

                    var isTop = placement <= _currentTargetPlace;
                    await _twitchIntegration.EndPredictionAsync(isTop);
                }

                if (Settings.Default.AutoRunAds)
                {
                    await _twitchIntegration.RunAdAsync(Settings.Default.AdTime);
                }
            }
            catch (Exception ex)
            {
                Hearthstone_Deck_Tracker.Utility.Logging.Log.Error($"Failed to end prediction or run ad: {ex.Message}");
            }
        }

        public void Dispose()
        {
        }
    }
}