﻿using Mixer.Base.Model.Channel;
using Mixer.Base.Model.MixPlay;
using Mixer.Base.Model.User;
using MixItUp.Base.Commands;
using MixItUp.Base.Model.API;
using MixItUp.Base.Model.Chat.Mixer;
using MixItUp.Base.Model.Settings;
using MixItUp.Base.Model.User;
using MixItUp.Base.Services;
using MixItUp.Base.Services.External;
using MixItUp.Base.Services.Mixer;
using MixItUp.Base.Util;
using MixItUp.Base.ViewModel.User;
using StreamingClient.Base.Model.OAuth;
using StreamingClient.Base.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MixItUp.Base
{
    public static class ChannelSession
    {
        public static MixerConnectionService MixerUserConnection { get; private set; }
        public static MixerConnectionService MixerBotConnection { get; private set; }

        public static PrivatePopulatedUserModel MixerUser { get; private set; }
        public static PrivatePopulatedUserModel MixerBot { get; private set; }
        public static ExpandedChannelModel MixerChannel { get; private set; }

        public static ApplicationSettingsV2Model AppSettings { get; private set; }
        public static SettingsV2Model Settings { get; private set; }

        public static ServicesManagerBase Services { get; private set; }

        public static List<PreMadeChatCommand> PreMadeChatCommands { get; private set; }

        private static CancellationTokenSource sessionBackgroundCancellationTokenSource = new CancellationTokenSource();
        private static int sessionBackgroundTimer = 0;

        public static bool IsDebug()
        {
            #if DEBUG
                return true;
            #else
                return false;
            #endif
        }

        public static bool IsElevated { get; set; }

        public static IEnumerable<PermissionsCommandBase> AllEnabledChatCommands
        {
            get
            {
                return ChannelSession.AllChatCommands.Where(c => c.IsEnabled);
            }
        }

        public static IEnumerable<PermissionsCommandBase> AllChatCommands
        {
            get
            {
                List<PermissionsCommandBase> commands = new List<PermissionsCommandBase>();
                commands.AddRange(ChannelSession.PreMadeChatCommands);
                commands.AddRange(ChannelSession.Settings.ChatCommands);
                commands.AddRange(ChannelSession.Settings.GameCommands);
                return commands;
            }
        }

        public static IEnumerable<CommandBase> AllEnabledCommands
        {
            get
            {
                return ChannelSession.AllCommands.Where(c => c.IsEnabled);
            }
        }

        public static IEnumerable<CommandBase> AllCommands
        {
            get
            {
                List<CommandBase> commands = new List<CommandBase>();
                commands.AddRange(ChannelSession.AllChatCommands);
                commands.AddRange(ChannelSession.Settings.EventCommands);
                commands.AddRange(ChannelSession.Settings.MixPlayCommands);
                commands.AddRange(ChannelSession.Settings.TimerCommands);
                commands.AddRange(ChannelSession.Settings.ActionGroupCommands);
                return commands;
            }
        }

        public static bool IsStreamer
        {
            get
            {
                if (ChannelSession.MixerUser != null && ChannelSession.MixerChannel != null)
                {
                    return ChannelSession.MixerUser.id == ChannelSession.MixerChannel.user.id;
                }
                return false;
            }
        }

        public static async Task Initialize(ServicesManagerBase serviceHandler)
        {
            ChannelSession.Services = serviceHandler;

            try
            {
                Type mixItUpSecretsType = Type.GetType("MixItUp.Base.MixItUpSecrets");
                if (mixItUpSecretsType != null)
                {
                    ChannelSession.Services.SetSecrets((SecretsService)Activator.CreateInstance(mixItUpSecretsType));
                }
            }
            catch (Exception ex) { Logger.Log(ex); }

            ChannelSession.PreMadeChatCommands = new List<PreMadeChatCommand>();

            ChannelSession.AppSettings = await ApplicationSettingsV2Model.Load();
        }

        public static async Task<Result> ConnectMixerUser(bool isStreamer)
        {
            Result<MixerConnectionService> result = await MixerConnectionService.ConnectUser(isStreamer);
            if (result.Success)
            {
                ChannelSession.MixerUserConnection = result.Value;
                ChannelSession.MixerUser = await ChannelSession.MixerUserConnection.GetCurrentUser();
                if (ChannelSession.MixerUser == null)
                {
                    return new Result("Failed to get Mixer user data");
                }
            }
            return result;
        }

        public static async Task<Result> ConnectMixerBot()
        {
            Result<MixerConnectionService> result = await MixerConnectionService.ConnectBot();
            if (result.Success)
            {
                ChannelSession.MixerBotConnection = result.Value;
                ChannelSession.MixerBot = await ChannelSession.MixerBotConnection.GetCurrentUser();
                if (ChannelSession.MixerBot == null)
                {
                    return new Result("Failed to get Mixer bot data");
                }

                if (ChannelSession.Services.Chat.MixerChatService != null && ChannelSession.Services.Chat.MixerChatService.IsUserConnected)
                {
                    return await ChannelSession.Services.Chat.MixerChatService.ConnectBot();
                }
            }
            return result;
        }

        public static async Task<Result> ConnectUser(SettingsV2Model settings)
        {
            Result userResult = null;
            ChannelSession.Settings = settings;

            Result<MixerConnectionService> result = await MixerConnectionService.Connect(ChannelSession.Settings.MixerUserOAuthToken);
            if (result.Success)
            {
                ChannelSession.MixerUserConnection = result.Value;
                userResult = result;
            }
            else
            {
                userResult = await ChannelSession.ConnectMixerUser(ChannelSession.Settings.IsStreamer);
            }

            if (userResult.Success)
            {
                ChannelSession.MixerUser = await ChannelSession.MixerUserConnection.GetCurrentUser();
                if (ChannelSession.MixerUser == null)
                {
                    return new Result("Failed to get Mixer user data");
                }

                if (settings.MixerBotOAuthToken != null)
                {
                    result = await MixerConnectionService.Connect(settings.MixerBotOAuthToken);
                    if (result.Success)
                    {
                        ChannelSession.MixerBotConnection = result.Value;
                        ChannelSession.MixerBot = await ChannelSession.MixerBotConnection.GetCurrentUser();
                        if (ChannelSession.MixerBot == null)
                        {
                            return new Result("Failed to get Mixer bot data");
                        }
                    }
                    else
                    {
                        settings.MixerBotOAuthToken = null;
                        return new Result(success: true, message: "Failed to connect Mixer bot account, please manually reconnect");
                    }
                }
            }
            return new Result();
        }

        public static async Task DisconnectMixerBot()
        {
            ChannelSession.MixerBotConnection = null;
            if (ChannelSession.Services.Chat.MixerChatService != null)
            {
                await ChannelSession.Services.Chat.MixerChatService.DisconnectBot();
            }
        }

        public static async Task Close()
        {
            await ChannelSession.Services.Close();
            if (ChannelSession.Services.Chat.MixerChatService != null)
            {
                await ChannelSession.Services.Chat.MixerChatService.DisconnectUser();
            }
            await ChannelSession.DisconnectMixerBot();
        }

        public static async Task SaveSettings()
        {
            await ChannelSession.Services.Settings.Save(ChannelSession.Settings);
        }

        public static async Task RefreshUser()
        {
            if (ChannelSession.MixerUser != null)
            {
                PrivatePopulatedUserModel user = await ChannelSession.MixerUserConnection.GetCurrentUser();
                if (user != null)
                {
                    ChannelSession.MixerUser = user;
                }
            }
        }

        public static async Task RefreshChannel()
        {
            if (ChannelSession.MixerChannel != null)
            {
                ExpandedChannelModel channel = await ChannelSession.MixerUserConnection.GetChannel(ChannelSession.MixerChannel.id);
                if (channel != null)
                {
                    ChannelSession.MixerChannel = channel;
                }
            }
        }

        public static UserViewModel GetCurrentUser()
        {
            UserViewModel user = ChannelSession.Services.User.GetUserByMixerID(ChannelSession.MixerUser.id);
            if (user == null)
            {
                user = new UserViewModel(ChannelSession.MixerUser);
            }
            return user;
        }

        public static void DisconnectionOccurred(string serviceName)
        {
            Logger.Log(serviceName + " Service disconnection occurred");
            GlobalEvents.ServiceDisconnect(serviceName);
        }

        public static void ReconnectionOccurred(string serviceName)
        {
            Logger.Log(serviceName + " Service reconnection successful");
            GlobalEvents.ServiceReconnect(serviceName);
        }

        public static async Task<bool> InitializeSession(string modChannelName = null)
        {
            try
            {
                ExpandedChannelModel mixerChannel = null;
                if (modChannelName == null)
                {
                    mixerChannel = await ChannelSession.MixerUserConnection.GetChannel(ChannelSession.MixerUser.channel.id);
                }
                else
                {
                    mixerChannel = await ChannelSession.MixerUserConnection.GetChannel(modChannelName);
                }

                if (mixerChannel != null)
                {
                    ChannelSession.MixerChannel = mixerChannel;

                    if (ChannelSession.Settings == null)
                    {
                        IEnumerable<SettingsV2Model> currentSettings = await ChannelSession.Services.Settings.GetAllSettings();
                        if (currentSettings.Any(s => s.MixerChannelID > 0 && s.MixerChannelID == mixerChannel.id))
                        {
                            GlobalEvents.ShowMessageBox($"There already exists settings for the account {mixerChannel.token}. Please sign in with a different account or re-launch Mix It Up to select those settings from the drop-down.");
                            return false;
                        }
                        ChannelSession.Settings = await ChannelSession.Services.Settings.Create(mixerChannel, modChannelName == null);
                    }
                    await ChannelSession.Services.Settings.Initialize(ChannelSession.Settings);

                    if (ChannelSession.Settings.DiagnosticLogging)
                    {
                        Logger.SetLogLevel(LogLevel.Debug);
                    }
                    else
                    {
                        Logger.SetLogLevel(LogLevel.Error);
                    }

                    if (modChannelName == null && ChannelSession.Settings.MixerChannelID > 0 && ChannelSession.MixerUser.channel.id != ChannelSession.Settings.MixerChannelID)
                    {
                        GlobalEvents.ShowMessageBox("The account you are logged in as on Mixer does not match the account for this settings. Please log in as the correct account on Mixer.");
                        ChannelSession.Settings.MixerUserOAuthToken.accessToken = string.Empty;
                        ChannelSession.Settings.MixerUserOAuthToken.refreshToken = string.Empty;
                        ChannelSession.Settings.MixerUserOAuthToken.expiresIn = 0;
                        return false;
                    }

                    ChannelSession.Settings.MixerChannelID = mixerChannel.id;

                    await ChannelSession.Services.Telemetry.Connect();
                    ChannelSession.Services.Telemetry.SetUserID(ChannelSession.Settings.TelemetryUserID);

                    MixerChatService mixerChatService = new MixerChatService();
                    MixerEventService mixerEventService = new MixerEventService();

                    List<Task<Result>> mixerConnections = new List<Task<Result>>();
                    mixerConnections.Add(mixerChatService.ConnectUser());

                    Task<Result> mixerEventServiceResult = mixerEventService.Connect();
                    mixerConnections.Add(mixerEventServiceResult);

                    await Task.WhenAll(mixerConnections);

                    if (mixerConnections.Any(c => !c.Result.Success))
                    {
                        string errors = string.Join(Environment.NewLine, mixerConnections.Where(c => !c.Result.Success).Select(c => c.Result.Message));
                        string message = "Failed to connect to Mixer services:" + Environment.NewLine + Environment.NewLine + errors + Environment.NewLine + Environment.NewLine + "This may be due to a Mixer server outage, please check Mixer's status page for more information: https://status.mixer.com/";

                        if (mixerConnections.All(c => c.Result.Success || c == mixerEventServiceResult))
                        {
                            if (!await DialogHelper.ShowConfirmation(message + Environment.NewLine + Environment.NewLine +
                                    "We have determined this to be a non-blocking error, which means we can attempt to log you in and ignore this. However, some features may not work as a result and you may run into some bugs."
                                    + Environment.NewLine + Environment.NewLine + "Would you like to ignore this and log in?"))
                            {
                                return false;
                            }
                        }
                        else
                        {
                            GlobalEvents.ShowMessageBox(message);
                            return false;
                        }
                    }

                    await ChannelSession.Services.Chat.Initialize(mixerChatService);
                    await ChannelSession.Services.Events.Initialize(mixerEventService);

                    await MixerChatEmoteModel.InitializeEmoteCache();

                    if (ChannelSession.IsStreamer)
                    {
                        if (!await ChannelSession.InitializeBotInternal())
                        {
                            await DialogHelper.ShowMessage("Failed to initialize Bot account");
                            return false;
                        }

                        // Connect External Services
                        Dictionary<IExternalService, OAuthTokenModel> externalServiceToConnect = new Dictionary<IExternalService, OAuthTokenModel>();
                        if (ChannelSession.Settings.StreamlabsOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Streamlabs] = ChannelSession.Settings.StreamlabsOAuthToken; }
                        if (ChannelSession.Settings.StreamElementsOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.StreamElements] = ChannelSession.Settings.StreamElementsOAuthToken; }
                        if (ChannelSession.Settings.StreamJarOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.StreamJar] = ChannelSession.Settings.StreamJarOAuthToken; }
                        if (ChannelSession.Settings.TipeeeStreamOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.TipeeeStream] = ChannelSession.Settings.TipeeeStreamOAuthToken; }
                        if (ChannelSession.Settings.TreatStreamOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.TreatStream] = ChannelSession.Settings.TreatStreamOAuthToken; }
                        if (ChannelSession.Settings.StreamlootsOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Streamloots] = ChannelSession.Settings.StreamlootsOAuthToken; }
                        if (ChannelSession.Settings.TiltifyOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Tiltify] = ChannelSession.Settings.TiltifyOAuthToken; }
                        if (ChannelSession.Settings.JustGivingOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.JustGiving] = ChannelSession.Settings.JustGivingOAuthToken; }
                        if (ChannelSession.Settings.IFTTTOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.IFTTT] = ChannelSession.Settings.IFTTTOAuthToken; }
                        if (ChannelSession.Settings.ExtraLifeTeamID > 0) { externalServiceToConnect[ChannelSession.Services.ExtraLife] = new OAuthTokenModel(); }
                        if (ChannelSession.Settings.PatreonOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Patreon] = ChannelSession.Settings.PatreonOAuthToken; }
                        if (ChannelSession.Settings.DiscordOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Discord] = ChannelSession.Settings.DiscordOAuthToken; }
                        if (ChannelSession.Settings.TwitterOAuthToken != null) { externalServiceToConnect[ChannelSession.Services.Twitter] = ChannelSession.Settings.TwitterOAuthToken; }
                        if (!string.IsNullOrEmpty(ChannelSession.Settings.OBSStudioServerIP)) { externalServiceToConnect[ChannelSession.Services.OBSStudio] = null; }
                        if (ChannelSession.Settings.EnableStreamlabsOBSConnection) { externalServiceToConnect[ChannelSession.Services.StreamlabsOBS] = null; }
                        if (ChannelSession.Settings.EnableXSplitConnection) { externalServiceToConnect[ChannelSession.Services.XSplit] = null; }
                        if (!string.IsNullOrEmpty(ChannelSession.Settings.OvrStreamServerIP)) { externalServiceToConnect[ChannelSession.Services.OvrStream] = null; }
                        if (ChannelSession.Settings.EnableOverlay) { externalServiceToConnect[ChannelSession.Services.Overlay] = null; }
                        if (ChannelSession.Settings.EnableDeveloperAPI) { externalServiceToConnect[ChannelSession.Services.DeveloperAPI] = null; }

                        if (externalServiceToConnect.Count > 0)
                        {
                            Dictionary<IExternalService, Task<Result>> externalServiceTasks = new Dictionary<IExternalService, Task<Result>>();
                            foreach (var kvp in externalServiceToConnect)
                            {
                                Logger.Log(LogLevel.Debug, "Trying automatic OAuth service connection: " + kvp.Key.Name);

                                if (kvp.Key is IOAuthExternalService && kvp.Value != null)
                                {
                                    externalServiceTasks[kvp.Key] = ((IOAuthExternalService)kvp.Key).Connect(kvp.Value);
                                }
                                else
                                {
                                    externalServiceTasks[kvp.Key] = kvp.Key.Connect();
                                }
                            }
                            await Task.WhenAll(externalServiceTasks.Values);

                            List<IExternalService> failedServices = new List<IExternalService>();
                            foreach (var kvp in externalServiceTasks)
                            {
                                if (!kvp.Value.Result.Success && kvp.Key is IOAuthExternalService)
                                {
                                    Logger.Log(LogLevel.Debug, "Automatic OAuth token connection failed, trying manual connection: " + kvp.Key.Name);

                                    Result result = await kvp.Key.Connect();
                                    if (!result.Success)
                                    {
                                        failedServices.Add(kvp.Key);
                                    }
                                }
                            }

                            if (failedServices.Count > 0)
                            {
                                Logger.Log(LogLevel.Debug, "Connection failed for services: " + string.Join(", ", failedServices.Select(s => s.Name)));

                                StringBuilder message = new StringBuilder();
                                message.AppendLine("The following services could not be connected:");
                                message.AppendLine();
                                foreach (IExternalService service in failedServices)
                                {
                                    message.AppendLine(" - " + service.Name);
                                }
                                message.AppendLine();
                                message.Append("Please go to the Services page to reconnect them manually.");
                                await DialogHelper.ShowMessage(message.ToString());
                            }
                        }

                        if (ChannelSession.Settings.RemoteHostConnection != null)
                        {
                            await ChannelSession.Services.RemoteService.InitializeConnection(ChannelSession.Settings.RemoteHostConnection);
                        }

                        if (ChannelSession.Settings.DefaultMixPlayGame > 0)
                        {
                            IEnumerable<MixPlayGameListingModel> games = await ChannelSession.MixerUserConnection.GetOwnedMixPlayGames(ChannelSession.MixerChannel);
                            MixPlayGameListingModel game = games.FirstOrDefault(g => g.id.Equals(ChannelSession.Settings.DefaultMixPlayGame));
                            if (game != null)
                            {
                                await ChannelSession.Services.MixPlay.SetGame(game);
                                Result result = await ChannelSession.Services.MixPlay.Connect();
                                if (!result.Success)
                                {
                                    await ChannelSession.Services.MixPlay.Disconnect();
                                }
                            }
                            else
                            {
                                ChannelSession.Settings.DefaultMixPlayGame = 0;
                            }
                        }

                        foreach (UserCurrencyModel currency in ChannelSession.Settings.Currencies.Values)
                        {
                            if (currency.ShouldBeReset())
                            {
                                await currency.Reset();
                            }
                        }

                        if (ChannelSession.Settings.ModerationResetStrikesOnLaunch)
                        {
                            foreach (UserDataModel userData in ChannelSession.Settings.UserData.Values)
                            {
                                if (userData.ModerationStrikes > 0)
                                {
                                    userData.ModerationStrikes = 0;
                                    ChannelSession.Settings.UserData.ManualValueChanged(userData.ID);
                                }
                            }
                        }

                        ChannelSession.PreMadeChatCommands.Clear();
                        foreach (PreMadeChatCommand command in ReflectionHelper.CreateInstancesOfImplementingType<PreMadeChatCommand>())
                        {
#pragma warning disable CS0612 // Type or member is obsolete
                            if (!(command is ObsoletePreMadeCommand))
                            {
                                ChannelSession.PreMadeChatCommands.Add(command);
                            }
#pragma warning restore CS0612 // Type or member is obsolete
                        }

                        foreach (PreMadeChatCommandSettings commandSetting in ChannelSession.Settings.PreMadeChatCommandSettings)
                        {
                            PreMadeChatCommand command = ChannelSession.PreMadeChatCommands.FirstOrDefault(c => c.Name.Equals(commandSetting.Name));
                            if (command != null)
                            {
                                command.UpdateFromSettings(commandSetting);
                            }
                        }
                        ChannelSession.Services.Chat.RebuildCommandTriggers();

                        ChannelSession.Services.TimerService.Initialize();
                        await ChannelSession.Services.Moderation.Initialize();
                    }

                    ChannelSession.Services.Statistics.Initialize();

                    ChannelSession.Services.InputService.HotKeyPressed += InputService_HotKeyPressed;

                    await ChannelSession.SaveSettings();
                    await ChannelSession.Services.Settings.SaveLocalBackup(ChannelSession.Settings);
                    await ChannelSession.Services.Settings.PerformAutomaticBackupIfApplicable(ChannelSession.Settings);

                    ChannelSession.Services.Telemetry.TrackLogin(ChannelSession.MixerUser.id.ToString(), ChannelSession.IsStreamer, ChannelSession.MixerChannel.partnered);
                    if (ChannelSession.Settings.IsStreamer)
                    {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                        Task.Run(async () => { await ChannelSession.Services.MixItUpService.SendUserFeatureEvent(new UserFeatureEvent(ChannelSession.MixerUser.id)); });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                    }

                    GlobalEvents.OnRankChanged += GlobalEvents_OnRankChanged;

                    AsyncRunner.RunBackgroundTask(sessionBackgroundCancellationTokenSource.Token, 60000, SessionBackgroundTask);

                    return true;
                }
            }
            catch (Exception ex)
            {
                await DialogHelper.ShowMessage("An error occurred while trying to initialize your session. If this continues, please visit the Mix It Up Discord for assistance." +
                    Environment.NewLine + Environment.NewLine + "Error Details: " + ex.Message);

                Logger.Log(ex);
            }
            return false;
        }

        private static async Task<bool> InitializeBotInternal()
        {
            if (ChannelSession.MixerBotConnection != null)
            {
                PrivatePopulatedUserModel user = await ChannelSession.MixerBotConnection.GetCurrentUser();
                if (user != null)
                {
                    ChannelSession.MixerBot = user;

                    Result result = await ChannelSession.Services.Chat.MixerChatService.ConnectBot();
                    return result.Success;
                }
                return false;
            }
            return true;
        }

        private static async Task SessionBackgroundTask(CancellationToken cancellationToken)
        {
            if (!cancellationToken.IsCancellationRequested)
            {
                sessionBackgroundTimer++;

                await ChannelSession.RefreshUser();

                await ChannelSession.RefreshChannel();

                if (sessionBackgroundTimer >= 5)
                {
                    await ChannelSession.SaveSettings();
                    sessionBackgroundTimer = 0;
                }
            }
        }

        private static async void GlobalEvents_OnRankChanged(object sender, UserCurrencyDataViewModel currency)
        {
            if (currency.Currency.RankChangedCommand != null)
            {
                UserViewModel user = ChannelSession.Services.User.GetUserByMixerID(currency.User.MixerID);
                if (user != null)
                {
                    await currency.Currency.RankChangedCommand.Perform(user);
                }
            }
        }

        private static async void InputService_HotKeyPressed(object sender, HotKey hotKey)
        {
            if (ChannelSession.Settings.HotKeys.ContainsKey(hotKey.ToString()))
            {
                HotKeyConfiguration hotKeyConfiguration = ChannelSession.Settings.HotKeys[hotKey.ToString()];
                CommandBase command = ChannelSession.AllCommands.FirstOrDefault(c => c.ID.Equals(hotKeyConfiguration.CommandID));
                if (command != null)
                {
                    await command.Perform();
                }
            }
        }
    }
}