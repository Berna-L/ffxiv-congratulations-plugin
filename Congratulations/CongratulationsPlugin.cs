using System;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Congratulations.Windows;
using Dalamud.Game;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace Congratulations
{
    public sealed class CongratulationsPlugin : IDalamudPlugin
    {
        public string Name => "Congratulations!";
        private const string CommandName = "/congratsconfig";

        public Configuration Configuration { get; init; }

        public readonly WindowSystem WindowSystem = new("Congratulations");

        private short lastCommendationCount;
        private int largestPartySize;
        private int currentPartySize;
        private int lastAreaPartySize;
        private ConfigWindow configWindow;

        public CongratulationsPlugin(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            this.Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(Service.PluginInterface);
            Configuration.Save();

            configWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(configWindow);

            Service.CommandManager.AddHandler(CommandName, new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Opens the Congratulations configuration window"
            });
            Service.PluginInterface.UiBuilder.Draw += DrawUserInterface;
            Service.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigWindow;

            Service.ClientState.TerritoryChanged += OnTerritoryChange;
            Service.Framework.Update += OnUpdate;
            Service.ClientState.Login += OnLogin;

            if (Service.ClientState.IsLoggedIn)
            {
                OnLogin();
            }
        }

        private void OnLogin()
        {
            this.lastCommendationCount = GetCurrentCommendationCount();
            Service.PluginLog.Debug("Starting commendations: {0}", lastCommendationCount);

            currentPartySize = GetCurrentPartySize();
            lastAreaPartySize = currentPartySize;
            largestPartySize = currentPartySize;
            Service.PluginLog.Debug("Starting party size: {0}", largestPartySize);
        }

        //Called each frame or something?
        private void OnUpdate(IFramework framework)
        {
            if (!Service.ClientState.IsLoggedIn) return;
            currentPartySize = GetCurrentPartySize();
            // If the current party size is bigger than it was last update, we update the largest party size
            if (currentPartySize > largestPartySize)
            {
                Service.PluginLog.Debug("Party grew from {0} to {1}", largestPartySize, currentPartySize);
                largestPartySize = currentPartySize;
            }
        }

        // Called whenever the WoL changes location (e.g. from the world to an instanced duty)
        // BTW, the party is formed/dissolved *after* this is called, which explains the somewhat
        // weird logic that happens here.
        private void OnTerritoryChange(ushort @ushort)
        {
            if (!Service.ClientState.IsLoggedIn) return;
            Service.PluginLog.Debug("territory changed");
            var currentCommendationCount = GetCurrentCommendationCount();

            // If the WoL commendations went up when changing location
            // (i.e. a duty has finished and the WoL left the instance)
            if (currentCommendationCount > lastCommendationCount)
            {
                Service.PluginLog.Debug("Commends changed from {0} to {1}", lastCommendationCount, currentCommendationCount);
                // lastAreaPartySize = party size BEFORE joining the duty (that can't commend you).
                // largestPartySize = party size INSIDE the duty (including those that can and can't commend you).
                // the remainder is the number of matchmade players.
                PlayCongratulations(largestPartySize - lastAreaPartySize,
                                    currentCommendationCount - lastCommendationCount);
            }

            // In any case, update the cached values.
            lastCommendationCount = currentCommendationCount;
            lastAreaPartySize = currentPartySize;
            currentPartySize = GetCurrentPartySize();
            Service.PluginLog.Debug("Party size reset from {0} to {1}", largestPartySize, currentPartySize);
            largestPartySize = currentPartySize;
        }

        private int GetCurrentPartySize()
        {
            // PartyList.Length returns 0 if the player is alone,
            // so we change it to 1 manually if that's the case.
            return Math.Max(Service.PartyList.Length, 1);
        }

        private static unsafe short GetCurrentCommendationCount()
        {
            return PlayerState.Instance()->PlayerCommendations;
        }
        
        private void PlayCongratulations(int numberOfMatchMadePlayers, int commendsObtained)
        {
            Service.PluginLog.Debug("Playing sound for {0} commends obtained of a maximum of {1}", commendsObtained,
                              numberOfMatchMadePlayers);

            void Func(Configuration.SubConfiguration config)
            {
                if (config.PlaySound)
                {
                    SoundEngine.PlaySound(config.GetFilePath(), config.ApplySfxVolume, config.Volume * 0.01f);
                }
            }

            if (commendsObtained == 7)
            {
                Func(Configuration.AllSevenInAFullParty);
            }
            else
            {
                var normalizedCommends = commendsObtained / (numberOfMatchMadePlayers * 1.0f);
                switch (normalizedCommends)
                {
                    case > 2 / 3f:
                        Func(Configuration.ThreeThirds);
                        break;
                    case > 1 / 3f:
                        Func(Configuration.TwoThirds);
                        break;
                    case > 0:
                        Func(Configuration.OneThird);
                        break;
                }
            }
        }


        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            Service.CommandManager.RemoveHandler(CommandName);
            Service.ClientState.TerritoryChanged -= OnTerritoryChange;
            Service.Framework.Update -= OnUpdate;
            Service.ClientState.Login -= OnLogin;
        }

        private void OnConfigCommand(string command, string args)
        {
            // in response to the slash command, just display our main ui
            configWindow.IsOpen = true;
        }

        private void DrawUserInterface()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigWindow()
        {
            configWindow!.IsOpen = true;
        }
    }
}
