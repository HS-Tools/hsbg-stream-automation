using System.Windows.Controls;
using Hearthstone_Deck_Tracker.Plugins;
using HSBG_Ads_Predictions_for_Twitch.Settings;

namespace HSBG_Ads_Predictions_for_Twitch.Controls
{
    public partial class PluginConfig : UserControl, IPluginConfigurable
    {
        private HSBGPluginSettings _settings;

        public PluginConfig()
        {
            InitializeComponent();
            _settings = HSBGPluginSettings.LoadSettings();
            DataContext = _settings;
        }

        public UserControl ConfigPanel => this;

        public void SaveSettings()
        {
            _settings.SaveSettings();
        }
    }
} 