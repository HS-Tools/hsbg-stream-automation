using System;
using System.ComponentModel;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker;

namespace HSBG_Ads_Predictions_for_Twitch.Settings
{
    [Serializable]
    public class HSBGPluginSettings : PluginSettings
    {
        private static HSBGPluginSettings _instance;
        
        public static HSBGPluginSettings Default
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HSBGPluginSettings();
                }
                return _instance;
            }
        }

        [DefaultValue(30)]
        public int AdDurationSeconds { get; set; }

        [DefaultValue("4,3,2")]
        public string PredictionPlacements { get; set; }

        public HSBGPluginSettings()
        {
            AdDurationSeconds = 30;
            PredictionPlacements = "4,3,2";
        }

        public static HSBGPluginSettings LoadSettings()
        {
            return Config.Instance.PluginSettings.LoadSettings<HSBGPluginSettings>() ?? new HSBGPluginSettings();
        }

        public void SaveSettings()
        {
            Config.Instance.PluginSettings.SaveSettings(this);
            Config.Save();
        }

        public int[] GetPredictionPlacementsArray()
        {
            try
            {
                var parts = PredictionPlacements.Split(',');
                var result = new int[parts.Length];
                for (int i = 0; i < parts.Length; i++)
                {
                    if (int.TryParse(parts[i].Trim(), out int value) && value >= 1 && value <= 8)
                    {
                        result[i] = value;
                    }
                }
                return result;
            }
            catch
            {
                return new[] { 4, 3, 2 }; // Default values if parsing fails
            }
        }
    }
} 