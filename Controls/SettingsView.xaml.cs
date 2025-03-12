using System.Collections.Generic;
using System.Linq;
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

        public SettingsView()
        {
            InitializeComponent();
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
                    item.IsSelected = savedChoices.Contains(item.Content.ToString());
                }
            }
        }

        private void AdTimeInput_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double?> e)
        {
            if (e.NewValue.HasValue)
            {
                Properties.Settings.Default.AdTime = (int)e.NewValue.Value;
                Properties.Settings.Default.Save();
            }
        }

        private void PredictionChoices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedItems = PredictionChoices.SelectedItems.Cast<ListBoxItem>()
                .Select(item => item.Content.ToString().Replace("Top ", "")) // Remove "Top " prefix
                .ToList();

            if (selectedItems.Count == 0) // Ensure at least one option is selected
            {
                // If nothing is selected, select Top 1 by default
                var firstItem = PredictionChoices.Items[0] as ListBoxItem;
                firstItem.IsSelected = true;
                return;
            }

            Properties.Settings.Default.PredictionChoices = new System.Collections.Specialized.StringCollection();
            Properties.Settings.Default.PredictionChoices.AddRange(selectedItems.ToArray());
            Properties.Settings.Default.Save();
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