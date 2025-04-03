using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Core = Hearthstone_Deck_Tracker.API.Core;
using System.Media;
using HSBG_Ads_Predictions_for_Twitch.Properties;
using System.Diagnostics;
using System.Windows.Threading;
using System.Configuration;

namespace HSBG_Ads_Predictions_for_Twitch.Controls
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : ScrollViewer
    {
        private static Flyout _flyout;

        public SettingsView()
        {
            InitializeComponent();
            
            // Check if we're already authenticated
            if (!string.IsNullOrEmpty(Properties.Settings.Default.AccessToken))
            {
                TwitchLoginPanel.Visibility = Visibility.Collapsed;
                ConnectedPanel.Visibility = Visibility.Visible;
                ConnectedChannelName.Text = "Connected to Twitch";
            }
            
            InitializeControls();
        }

        private void InitializeControls()
        {
            // Load saved prediction choices if they exist
            if (Properties.Settings.Default.PredictionChoices != null)
            {
                var savedChoices = Properties.Settings.Default.PredictionChoices.Cast<string>().ToList();
                foreach (ListBoxItem item in PredictionChoices.Items)
                {
                    var itemValue = item.Content.ToString();
                    item.IsSelected = savedChoices.Any(choice => itemValue.EndsWith(choice));
                }
            }

            // Update prediction checkbox based on choices
            UpdatePredictionCheckboxState();
        }

        private void UpdatePredictionCheckboxState()
        {
            var hasSelectedChoices = PredictionChoices?.SelectedItems.Count > 0;
            if (AutoRunPredictionsCheckbox != null)
            {
                AutoRunPredictionsCheckbox.IsChecked = hasSelectedChoices;
                Properties.Settings.Default.AutoRunPredictions = hasSelectedChoices;
                Properties.Settings.Default.Save();
            }
        }

        private void ConnectTwitchButton_Click(object sender, RoutedEventArgs e)
        {
            ConnectTwitchButton.IsEnabled = false;
            ConnectionStatus.Text = "Connecting to Twitch...";
            
            try 
            {
                // Launch browser with auth URL
                Process.Start("https://twitch-oauth-backend.vercel.app/api/start-twitch-auth");
                
                // For demo purposes, we'll simulate a successful auth
                // In a real implementation, you would handle the callback from the OAuth service
                Dispatcher.InvokeAsync(() => {
                    TwitchLoginPanel.Visibility = Visibility.Collapsed;
                    ConnectedPanel.Visibility = Visibility.Visible;
                    ConnectedChannelName.Text = "Connected as: TwitchUser";
                    
                    // Store a dummy token for testing
                    Properties.Settings.Default.AccessToken = "demo_token";
                    Properties.Settings.Default.Save();
                }, DispatcherPriority.Background);
            }
            catch (Exception ex)
            {
                ConnectionStatus.Text = $"Error: {ex.Message}";
                ConnectTwitchButton.IsEnabled = true;
            }
        }

        private void DisconnectButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear the stored tokens
            Properties.Settings.Default.AccessToken = "";
            //Properties.Settings.Default.RefreshToken = "";
            Properties.Settings.Default.Save();
            
            // Update UI
            TwitchLoginPanel.Visibility = Visibility.Visible;
            ConnectedPanel.Visibility = Visibility.Collapsed;
            ConnectionStatus.Text = "Not connected to Twitch";
            ConnectTwitchButton.IsEnabled = true;
        }

        private void ShowCredentialsButton_Click(object sender, RoutedEventArgs e)
        {
            string message = $"Client ID: {Properties.Settings.Default.ClientId}\n" +
                             $"Access Token: {Properties.Settings.Default.AccessToken}";
            
            // Show the credentials in a message box
            MessageBox.Show(message, "Current Credentials", MessageBoxButton.OK, MessageBoxImage.Information);
            
            // Also show the config file location
            string configPath = System.Configuration.ConfigurationManager.OpenExeConfiguration(
                System.Configuration.ConfigurationUserLevel.PerUserRoamingAndLocal).FilePath;
            
            MessageBox.Show($"Settings file location:\n{configPath}", 
                "Config File Location", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AutoRunAdsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkbox)
            {
                Properties.Settings.Default.AutoRunAds = checkbox.IsChecked ?? true;
                Properties.Settings.Default.Save();
            }
        }

        private void AutoRunPredictionsCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkbox && checkbox.IsChecked == true && PredictionChoices?.SelectedItems.Count == 0)
            {
                // If enabling predictions but no choices selected, select all choices
                foreach (ListBoxItem item in PredictionChoices.Items)
                {
                    item.IsSelected = true;
                }
            }
            if (sender is CheckBox cb)
            {
                Properties.Settings.Default.AutoRunPredictions = cb.IsChecked ?? true;
                Properties.Settings.Default.Save();
            }
        }

        private void PredictionChoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = PredictionChoices.SelectedItems.Cast<ListBoxItem>()
                .Select(item => item.Content.ToString().Replace("Top ", ""))
                .ToList();

            // Update AutoRunPredictions checkbox based on selection
            UpdatePredictionCheckboxState();

            if (selectedItems.Count > 0)
            {
                var collection = new System.Collections.Specialized.StringCollection();
                collection.AddRange(selectedItems.ToArray());
                Properties.Settings.Default.PredictionChoices = collection;
            }
            else
            {
                Properties.Settings.Default.PredictionChoices = null;
            }
            
            Properties.Settings.Default.Save();
        }

        private void ToggleCredentialsButton_Click(object sender, RoutedEventArgs e)
        {
            _credentialsVisible = !_credentialsVisible;
            CredentialsPanel.Visibility = _credentialsVisible ? Visibility.Visible : Visibility.Collapsed;
            ToggleCredentialsButton.Content = _credentialsVisible ? "Hide" : "Show";
        }

        private void ClientIdInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.ClientId = ClientIdInput.Text;
            Properties.Settings.Default.Save();
        }

        private void AccessTokenInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.AccessToken = AccessTokenInput.Text;
            Properties.Settings.Default.Save();
        }

        private void GetCredentialsButton_Click(object sender, RoutedEventArgs e)
        {
            // Open the Twitch Token Generator in the default browser
            var tokenGeneratorUrl = "https://twitchtokengenerator.com/quick/T3AZGjYdBd";
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(tokenGeneratorUrl) { UseShellExecute = true });
        }

        private void AdTimeInput_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue.HasValue)
            {
                Properties.Settings.Default.AdTime = (int)e.NewValue.Value;
                Properties.Settings.Default.Save();
            }
        }

        private void PlaySoundCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkbox)
            {
                Properties.Settings.Default.PlaySoundOnPrediction = checkbox.IsChecked ?? false;
                Properties.Settings.Default.Save();
            }
        }

        public static Flyout Flyout
        {
            get
            {
                if (_flyout == null)
                {
                    _flyout = CreateSettingsFlyout();
                }
                return _flyout;
            }
        }

        private static Flyout CreateSettingsFlyout()
        {
            var settings = new Flyout();
            settings.Position = Position.Left;
            Panel.SetZIndex(settings, 100);
            settings.Header = LocalizeTools.GetLocalized("LabelSettings");
            settings.Content = new SettingsView();
            Core.MainWindow.Flyouts.Items.Add(settings);
            return settings;
        }
    }
}