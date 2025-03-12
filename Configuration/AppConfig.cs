using Newtonsoft.Json;
using System.IO;

namespace HSBG_Ads_Predictions_for_Twitch.Configuration
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
} 