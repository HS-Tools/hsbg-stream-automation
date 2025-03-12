using Hearthstone_Deck_Tracker.API;
using HSBG_Ads_Predictions_for_Twitch.Controls;
using HSBG_Ads_Predictions_for_Twitch.Logic;
using HSBG_Ads_Predictions_for_Twitch.Properties;
using System;
using System.Windows.Controls;
using System.Windows.Media;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Windows;
using System.IO;
using Core = Hearthstone_Deck_Tracker.API.Core;
using Hearthstone_Deck_Tracker.Enums;
using HearthDb.Enums;

namespace HSBG_Ads_Predictions_for_Twitch
{
    public class AppConfig
    {
        public TwitchSettings Twitch { get; set; }
        public PredictionSettings Predictions { get; set; }
        public AdSettings Ads { get; set; }

        public static AppConfig LoadFromFile(string path = "config.json")
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Configuration file not found at: {path}");
            }

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<AppConfig>(json);
        }
    }

    public class TwitchSettings
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string OAuthToken { get; set; }
        public string BroadcasterId { get; set; }
    }

    public class PredictionSettings
    {
        public int[] PossiblePlacements { get; set; }
        public int DurationSeconds { get; set; }
    }

    public class AdSettings
    {
        public int DurationSeconds { get; set; }
    }

    /// <summary>
    /// This is where we put the logic for our Plug-in
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class TwitchConfig
    {
        public string ClientId { get; private set; }
        public string OAuthToken { get; private set; }

        public TwitchConfig(TwitchSettings settings)
        {
            ClientId = settings.ClientId;
            OAuthToken = settings.OAuthToken;
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

        public async Task<List<Prediction>> GetActivePredictionsAsync()
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

        public async Task CancelPredictionAsync(string predictionId)
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

        public async Task CancelActivePredictionsAsync()
        {
            var activePredictions = await GetActivePredictionsAsync();
            foreach (var prediction in activePredictions)
            {
                await CancelPredictionAsync(prediction.Id);
            }
        }

        public class PredictionResponse
        {
            public List<Prediction> Data { get; set; }
        }

        public class Prediction
        {
            public string Id { get; set; }
            public string Title { get; set; }
            public string Status { get; set; }
            public List<Outcome> Outcomes { get; set; }
        }

        public class Outcome
        {
            public string Id { get; set; }
            public string Title { get; set; }
        }

        public class UserResponse
        {
            public List<UserData> Data { get; set; }
        }

        public class UserData
        {
            public string Id { get; set; }
            public string Login { get; set; }
            public string DisplayName { get; set; }
        }
    }

    public class HSBG_Ads_Predictions_for_Twitch : IDisposable
    {
        // ToDo: The window shouldn't be statically named
        private static string panelName = "pluginStackPanelView";

        /// <summary>
        /// The class that allows us to let the user move the panel
        /// </summary>
        public static InputMoveManager inputMoveManager;

        /// <summary>
        /// The panel reference we will display our plug-in magic within
        /// </summary>
        public PlugInDisplayControl stackPanel;

        private TwitchConfig _twitchConfig;
        private TwitchIntegration _twitchIntegration;
        private AppConfig _config;
        private bool _predictionStarted = false;
        private Random _random = new Random();
        private int _currentTargetPlace; // Store the current target placement

        /// <summary>
        /// Initializes a new instance of the <see cref="HSBG_Ads_Predictions_for_Twitch"/> class.
        /// </summary>
        public HSBG_Ads_Predictions_for_Twitch()
        {
            // We are adding the Panel here for simplicity.  It would be better to add it under InitLogic()
            InitViewPanel();
            LoadConfiguration();
            InitTwitch();

            GameEvents.OnGameStart.Add(OnGameStart);
            GameEvents.OnTurnStart.Add(OnTurnStart);
            GameEvents.OnGameEnd.Add(OnGameEnd);
        }

        private void LoadConfiguration()
        {
            try
            {
                var pluginDirectory = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "HearthstoneDeckTracker",
                    "Plugins",
                    "HSBG_Ads_Predictions_for_Twitch"
                );
                var configPath = Path.Combine(pluginDirectory, "config.json");
                
                if (!File.Exists(configPath))
                {
                    var exampleConfigPath = Path.Combine(pluginDirectory, "config.example.json");
                    if (File.Exists(exampleConfigPath))
                    {
                        File.Copy(exampleConfigPath, configPath);
                    }
                    else
                    {
                        throw new FileNotFoundException($"Neither config.json nor config.example.json found in {pluginDirectory}");
                    }
                }

                _config = AppConfig.LoadFromFile(configPath);
            }
            catch (Exception ex)
            {
                Hearthstone_Deck_Tracker.Utility.Logging.Log.Error($"Failed to load configuration: {ex.Message}");
                throw;
            }
        }

        private async void InitTwitch()
        {
            try
            {
                // Check if we have valid credentials
                if (string.IsNullOrEmpty(Settings.Default.ClientId) || string.IsNullOrEmpty(Settings.Default.AccessToken))
                {
                    Hearthstone_Deck_Tracker.Utility.Logging.Log.Info("Twitch credentials not configured. Please set them in the plugin settings.");
                    return;
                }

                var twitchSettings = new TwitchSettings
                {
                    ClientId = Settings.Default.ClientId,
                    OAuthToken = Settings.Default.AccessToken.TrimStart("Bearer ".ToCharArray()) // Remove Bearer prefix if present
                };

                _twitchConfig = new TwitchConfig(twitchSettings);
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

            try
            {
                if (_predictionStarted) return;
                _predictionStarted = true;

                // Check if predictions are enabled and we have choices selected
                if (!Settings.Default.AutoRunPredictions || 
                    Settings.Default.PredictionChoices == null || 
                    Settings.Default.PredictionChoices.Count == 0)
                {
                    return;
                }
                
                await _twitchIntegration.CancelActivePredictionsAsync();
                
                var heroName = Core.Game.Player.Hero.Card.LocalizedName ?? "Hero";
                
                // Get prediction choices from settings with better parsing
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
                var outcomes = new List<string> { "Yes", "No" };
                await _twitchIntegration.CreatePredictionAsync(title, outcomes, 120);
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
                // Only resolve prediction if predictions are enabled and we have a target place
                if (Settings.Default.AutoRunPredictions && _currentTargetPlace > 0)
                {
                    var playerEntity = Core.Game.Entities.Values.FirstOrDefault(x => x.IsPlayer);
                    var playerPlaceEntity = Core.Game.Entities.Values
                        .FirstOrDefault(e => e.GetTag(GameTag.PLAYER_ID) == playerEntity.GetTag(GameTag.PLAYER_ID)
                                        && e.HasTag(GameTag.PLAYER_LEADERBOARD_PLACE));

                    // Get player's final placement
                    var placement = playerPlaceEntity?.GetTag(GameTag.PLAYER_LEADERBOARD_PLACE) ??
                                        playerEntity.GetTag(GameTag.PLAYER_LEADERBOARD_PLACE);

                    // Resolve prediction based on placement and target place
                    var isTop = placement <= _currentTargetPlace;
                    await _twitchIntegration.EndPredictionAsync(isTop);
                }

                // Only run ad if auto-run ads is enabled
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

        /// <summary>
        /// Check the game type to see if our Plug-in is used.
        /// </summary>
        private void GameTypeCheck()
        {
            // ToDo : Enable toggle Props
            if (Core.Game.CurrentGameType == HearthDb.Enums.GameType.GT_RANKED ||
                Core.Game.CurrentGameType == HearthDb.Enums.GameType.GT_CASUAL ||
                Core.Game.CurrentGameType == HearthDb.Enums.GameType.GT_FSG_BRAWL ||
                Core.Game.CurrentGameType == HearthDb.Enums.GameType.GT_ARENA)
            {
                InitLogic();
            }
        }

        private void InitLogic()
        {
            // Here you can begin to work your Plug-in magic
        }

        private void InitViewPanel()
        {
            stackPanel = new PlugInDisplayControl();
            stackPanel.Name = panelName;
            stackPanel.Visibility = System.Windows.Visibility.Collapsed;
            Core.OverlayCanvas.Children.Add(stackPanel);

            Canvas.SetTop(stackPanel, Settings.Default.Top);
            Canvas.SetLeft(stackPanel, Settings.Default.Left);

            inputMoveManager = new InputMoveManager(stackPanel);

            Settings.Default.PropertyChanged += SettingsChanged;
            SettingsChanged(null, null);
        }

        private void SettingsChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            stackPanel.RenderTransform = new ScaleTransform(Settings.Default.Scale / 100, Settings.Default.Scale / 100);
            stackPanel.Opacity = Settings.Default.Opacity / 100;
        }

        public void CleanUp()
        {
            if (stackPanel != null)
            {
                Core.OverlayCanvas.Children.Remove(stackPanel);
                Dispose();
            }
        }

        public void Dispose()
        {
        }
    }
}