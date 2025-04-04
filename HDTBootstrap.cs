﻿using Hearthstone_Deck_Tracker.Plugins;
using HSBG_Ads_Predictions_for_Twitch.Controls;
using HSBG_Ads_Predictions_for_Twitch.Properties;
using System;
using System.Reflection;
using System.Windows.Controls;
using HSBG_Ads_Predictions_for_Twitch.Configuration;

namespace HSBG_Ads_Predictions_for_Twitch
{
    /// <summary>
    /// Wires up your plug-ins' logic once HDT loads it in to the session.
    /// </summary>
    /// <seealso cref="Hearthstone_Deck_Tracker.Plugins.IPlugin" />
    public class HDTBootstrap : IPlugin
    {
        /// <summary>
        /// The Plug-in's running instance
        /// </summary>
        public HSBG_Ads_Predictions_for_Twitch pluginInstance;

        /// <summary>
        /// The author, so your name.
        /// </summary>
        /// <value>The author's name.</value>
        public string Author => "LiiHS";
// ToDo: put your name as the author


        public string ButtonText => "Settings";

       // ToDo: Update the Plug-in Description in StringsResource.resx        
       public string Description => "Automatically creates predictions and runs ads on Twitch for Hearthstone Battlegrounds Streamers";

        /// <summary>
        /// Gets or sets the main <see cref="MenuItem">Menu Item</see>.
        /// </summary>
        /// <value>The main <see cref="MenuItem">Menu Item</see>.</value>
        public MenuItem MenuItem { get; set; } = null;

        public string Name => "Automatic Predictions and Ads";

        /// <summary>
        /// The gets plug-in version.from the assembly
        /// </summary>
        /// <value>The plug-in assembly version.</value>
        public Version Version => Assembly.GetExecutingAssembly().GetName().Version;

        /// <summary>
        /// Adds the menu item.
        /// </summary>
        private void AddMenuItem()
        {
            this.MenuItem = new MenuItem()
            {
                Header = Name
            };

            this.MenuItem.Click += (sender, args) =>
            {
                OnButtonPress();
            };
        }

        public void OnButtonPress() => SettingsView.Flyout.IsOpen = true;

        public void OnLoad()
        {
            // Load settings from persistent storage first
            PersistentSettings.LoadSettings();
            
            pluginInstance = new HSBG_Ads_Predictions_for_Twitch();
            AddMenuItem();
        }

        /// <summary>
        /// Called when during the window clean-up.
        /// </summary>
        public void OnUnload()
        {
            // Save settings to both default storage and our persistent storage
            Settings.Default.Save();
            PersistentSettings.SaveSettings();

            pluginInstance = null;
        }

        /// <summary>
        /// Called when [update].
        /// </summary>
        public void OnUpdate()
        { }
    }
}