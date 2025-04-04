using Newtonsoft.Json;
using System;
using System.Collections.Specialized;
using System.IO;
using HSBG_Ads_Predictions_for_Twitch.Properties;

namespace HSBG_Ads_Predictions_for_Twitch.Configuration
{
    public class PersistentSettings
    {
        private static readonly string SettingsFileName = "hsbg-stream-automation.json";
        private static readonly string SettingsFilePath;

        // Settings properties that mirror those in Settings.settings
        public double Scale { get; set; } = 100;
        public double Opacity { get; set; } = 100;
        public double Top { get; set; } = 10;
        public double Left { get; set; } = 30;
        public int AdTime { get; set; } = 90;
        public string[] PredictionChoices { get; set; } = new[] { "1, 2, 3, 4" };
        public string ClientId { get; set; } = "";
        public string AccessToken { get; set; } = "";
        public bool AutoRunAds { get; set; } = true;
        public bool AutoRunPredictions { get; set; } = true;
        public bool PlaySoundOnPrediction { get; set; } = false;

        // Static constructor to initialize the file path
        static PersistentSettings()
        {
            // Determine the plugin's location
            string pluginFolder = AppDomain.CurrentDomain.BaseDirectory;
            
            // Get the parent directory of HDT
            string hdtFolder = Path.GetDirectoryName(pluginFolder);
            if (hdtFolder != null)
            {
                // Get the parent directory of the HDT folder
                string parentFolder = Path.GetDirectoryName(hdtFolder);
                if (parentFolder != null)
                {
                    // Go one more level up to be even safer from updates
                    string grandparentFolder = Path.GetDirectoryName(parentFolder);
                    if (grandparentFolder != null)
                    {
                        SettingsFilePath = Path.Combine(grandparentFolder, SettingsFileName);
                    }
                    else
                    {
                        // Fallback to parent folder if grandparent doesn't exist
                        SettingsFilePath = Path.Combine(parentFolder, SettingsFileName);
                    }
                }
                else
                {
                    // Fallback if parent folder not found
                    SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SettingsFileName);
                }
            }
            else
            {
                // Fallback if HDT folder not found
                SettingsFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), SettingsFileName);
            }
            
            // Log the settings file path for debugging
            Console.WriteLine($"Settings will be stored at: {SettingsFilePath}");
        }
        
        /// <summary>
        /// Load settings from persistent storage and apply to Settings.Default
        /// </summary>
        public static void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    var persistentSettings = JsonConvert.DeserializeObject<PersistentSettings>(json);
                    
                    if (persistentSettings != null)
                    {
                        // Copy values from persistent settings to application settings
                        Settings.Default.Scale = persistentSettings.Scale;
                        Settings.Default.Opacity = persistentSettings.Opacity;
                        Settings.Default.Top = persistentSettings.Top;
                        Settings.Default.Left = persistentSettings.Left;
                        Settings.Default.AdTime = persistentSettings.AdTime;
                        
                        // Handle string collection
                        if (persistentSettings.PredictionChoices != null && persistentSettings.PredictionChoices.Length > 0)
                        {
                            var stringCollection = new StringCollection();
                            stringCollection.AddRange(persistentSettings.PredictionChoices);
                            Settings.Default.PredictionChoices = stringCollection;
                        }
                        
                        Settings.Default.ClientId = persistentSettings.ClientId;
                        Settings.Default.AccessToken = persistentSettings.AccessToken;
                        Settings.Default.AutoRunAds = persistentSettings.AutoRunAds;
                        Settings.Default.AutoRunPredictions = persistentSettings.AutoRunPredictions;
                        Settings.Default.PlaySoundOnPrediction = persistentSettings.PlaySoundOnPrediction;
                        
                        // Save to application settings
                        Settings.Default.Save();
                        
                        Console.WriteLine($"Loaded settings from: {SettingsFilePath}");
                    }
                }
                else
                {
                    // If file doesn't exist, save current settings to create it
                    SaveSettings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Save current Settings.Default values to persistent storage
        /// </summary>
        public static void SaveSettings()
        {
            try
            {
                var persistentSettings = new PersistentSettings
                {
                    Scale = Settings.Default.Scale,
                    Opacity = Settings.Default.Opacity,
                    Top = Settings.Default.Top,
                    Left = Settings.Default.Left,
                    AdTime = Settings.Default.AdTime,
                    
                    // Convert StringCollection to string array
                    PredictionChoices = Settings.Default.PredictionChoices != null
                        ? ConvertStringCollectionToArray(Settings.Default.PredictionChoices)
                        : new[] { "1, 2, 3, 4" },
                    
                    ClientId = Settings.Default.ClientId,
                    AccessToken = Settings.Default.AccessToken,
                    AutoRunAds = Settings.Default.AutoRunAds,
                    AutoRunPredictions = Settings.Default.AutoRunPredictions,
                    PlaySoundOnPrediction = Settings.Default.PlaySoundOnPrediction
                };
                
                string json = JsonConvert.SerializeObject(persistentSettings, Formatting.Indented);
                
                // Ensure directory exists
                string directory = Path.GetDirectoryName(SettingsFilePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                
                File.WriteAllText(SettingsFilePath, json);
                Console.WriteLine($"Saved settings to: {SettingsFilePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
        
        private static string[] ConvertStringCollectionToArray(StringCollection collection)
        {
            if (collection == null || collection.Count == 0)
                return new string[0];
                
            string[] result = new string[collection.Count];
            collection.CopyTo(result, 0);
            return result;
        }
    }
} 