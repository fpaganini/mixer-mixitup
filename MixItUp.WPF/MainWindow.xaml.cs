﻿using MixItUp.Base;
using MixItUp.WPF.Controls.MainControls;
using MixItUp.WPF.Properties;
using MixItUp.WPF.Util;
using MixItUp.WPF.Windows;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace MixItUp.WPF
{
    /// <summary>
    /// Interaction logic for StreamerWindow.xaml
    /// </summary>
    public partial class MainWindow : LoadingWindowBase
    {
        public string RestoredSettingsFilePath = null;

        private bool restartApplication = false;

        private bool shutdownStarted = false;
        private bool shutdownComplete = false;

        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
            this.Initialize(this.StatusBar);
        }

        public void Restart()
        {
            this.restartApplication = true;
            this.Close();
        }

        public void ReRunWizard()
        {
            ChannelSession.Settings.ReRunWizard = true;
            this.Restart();
        }

        protected override async Task OnLoaded()
        {
            if (ChannelSession.Settings.IsStreamer)
            {
                this.Title += " - Streamer";
            }
            else
            {
                this.Title += " - Moderator";
            }
            this.Title += " - v" + Assembly.GetEntryAssembly().GetName().Version.ToString();

            await this.MainMenu.Initialize(this);

            await this.MainMenu.AddMenuItem("Chat", new ChatControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Chat");
            if (ChannelSession.Settings.IsStreamer)
            {
                await this.MainMenu.AddMenuItem("Channel", new ChannelControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Channel");
                await this.MainMenu.AddMenuItem("Commands", new ChatCommandsControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Commands");
                await this.MainMenu.AddMenuItem("Interactive", new InteractiveControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Interactive");
                await this.MainMenu.AddMenuItem("Events", new EventsControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Events");
                await this.MainMenu.AddMenuItem("Timers", new TimerControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Timers");
                await this.MainMenu.AddMenuItem("Action Groups", new ActionGroupControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Action-Groups");
                await this.MainMenu.AddMenuItem("Remote", new RemoteControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Remote");
                await this.MainMenu.AddMenuItem("Currency & Rank", new CurrencyAndRankControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Currency-&-Rank");
                await this.MainMenu.AddMenuItem("Games", new GamesControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Games");
                await this.MainMenu.AddMenuItem("Giveaway", new GiveawayControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Giveaways");
                await this.MainMenu.AddMenuItem("Game Queue", new GameQueueControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Game-Queue");
                await this.MainMenu.AddMenuItem("Quotes", new QuoteControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Quotes");
            }
            await this.MainMenu.AddMenuItem("Statistics", new StatisticsControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Statistics");
            if (ChannelSession.Settings.IsStreamer)
            {
                await this.MainMenu.AddMenuItem("Moderation", new ModerationControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Moderation");
                await this.MainMenu.AddMenuItem("Services", new ServicesControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki/Services");
            }
            await this.MainMenu.AddMenuItem("About", new AboutControl(), "https://github.com/SaviorXTanren/mixer-mixitup/wiki");

            if (!string.IsNullOrEmpty(ChannelSession.Settings.OBSStudioServerIP) && !Settings.Default.OBSUpgrade211)
            {
                if (await MessageBoxHelper.ShowConfirmationDialog("A new version of the OBS Studio WebSocket is available, but this version only works with OBS Studio 21.1.0 or higher. If you do not have OBS Studio 21.1.0 installed, we recommend you update OBS Studio first. Would you like us to install the new version of the OBS Studio WebSocket?"))
                {
                    Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OBS\\obs-websocket-4.3.3-Windows-Installer.exe"));
                }
            }

            Settings.Default.OBSUpgrade211 = true;
            Settings.Default.Save();
        }

        private async void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!this.shutdownStarted)
            {
                e.Cancel = true;
                if (await MessageBoxHelper.ShowConfirmationDialog("Are you sure you wish to exit Mix It Up?"))
                {
                    this.shutdownStarted = true;
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    this.StartShutdownProcess();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                }
            }
            else if (!this.shutdownComplete)
            {
                e.Cancel = true;
            }
        }

        private async Task StartShutdownProcess()
        {
            this.ShuttingDownGrid.Visibility = Visibility.Visible;
            this.MainMenu.Visibility = Visibility.Collapsed;

            if (!string.IsNullOrEmpty(this.RestoredSettingsFilePath))
            {
                string settingsFilePath = ChannelSession.Services.Settings.GetFilePath(ChannelSession.Settings);
                string settingsFolder = Path.GetDirectoryName(settingsFilePath);
                using (ZipArchive zipFile = ZipFile.Open(this.RestoredSettingsFilePath, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in zipFile.Entries)
                    {
                        string filePath = Path.Combine(settingsFolder, entry.Name);
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                    }
                    zipFile.ExtractToDirectory(settingsFolder);
                }
            }
            else
            {
                if (!await ChannelSession.Services.Settings.SaveAndValidate(ChannelSession.Settings))
                {
                    await Task.Delay(1000);
                    await ChannelSession.Services.Settings.SaveAndValidate(ChannelSession.Settings);
                }
            }

            await ChannelSession.Close();

            this.shutdownComplete = true;

            this.Close();
            if (this.restartApplication)
            {
                Process.Start(Application.ResourceAssembly.Location);
            }
        }
    }
}
