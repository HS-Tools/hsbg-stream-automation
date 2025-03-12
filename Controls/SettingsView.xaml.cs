using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;
using Core = Hearthstone_Deck_Tracker.API.Core;

namespace HSBG_Ads_Predictions_for_Twitch.Controls
{
    /// <summary>
    /// Interaction logic for SettingsView.xaml
    /// </summary>
    public partial class SettingsView : ScrollViewer
    {
        private static Flyout _flyout;
        private bool _credentialsVisible = false;

        public SettingsView()
        {
            InitializeComponent();
            InitializeControls();
        }

        private void InitializeControls()
        {
            // Load Twitch credentials
            ClientIdInput.Text = Properties.Settings.Default.ClientId;
            AccessTokenInput.Text = Properties.Settings.Default.AccessToken;

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

        private void AdTimeInput_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue.HasValue)
            {
                Properties.Settings.Default.AdTime = (int)e.NewValue.Value;
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