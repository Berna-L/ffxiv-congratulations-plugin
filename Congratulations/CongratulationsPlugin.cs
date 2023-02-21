using System;
using Dalamud.Game.Command;
using Dalamud.Plugin;
using Congratulations.Windows;
using Dalamud.Game;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
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

        public CongratulationsPlugin(DalamudPluginInterface pluginInterface)
        {
            pluginInterface.Create<Service>();

            this.Configuration = Service.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(Service.PluginInterface);

            WindowSystem.AddWindow(new ConfigWindow(this));

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
                OnLogin(null, EventArgs.Empty);
            }
        }

        private void OnLogin(object? sender, EventArgs e)
        {
            this.lastCommendationCount = GetCurrentCommendationCount();
            PluginLog.LogDebug("Starting commendations: {0}", lastCommendationCount);

            currentPartySize = GetCurrentPartySize();
            lastAreaPartySize = currentPartySize;
            largestPartySize = currentPartySize;
            PluginLog.LogDebug("Starting party size: {0}", largestPartySize);
        }

        //Called each frame or something?
        private void OnUpdate(Framework framework)
        {
            if (!Service.ClientState.IsLoggedIn) return;
            currentPartySize = GetCurrentPartySize();
            // If the current party size is bigger than it was last update, we update the largest party size
            if (currentPartySize > largestPartySize)
            {
                PluginLog.LogDebug("Party grew from {0} to {1}", largestPartySize, currentPartySize);
                largestPartySize = currentPartySize;
            }
        }

        // Called whenever the WoL changes location (e.g. from the world to an instanced duty)
        // BTW, the party is formed/dissolved *after* this is called, which explains the somewhat
        // weird logic that happens here.
        private void OnTerritoryChange(object? sender, ushort @ushort)
        {
            if (!Service.ClientState.IsLoggedIn) return;
            PluginLog.LogDebug("territory changed");
            var currentCommendationCount = GetCurrentCommendationCount();

            // If the WoL commendations went up when changing location
            // (i.e. a duty has finished and the WoL left the instance)
            if (currentCommendationCount > lastCommendationCount)
            {
                PluginLog.Debug("Commends changed from {0} to {1}", lastCommendationCount, currentCommendationCount);
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
            PluginLog.Debug("Party size reset from {0} to {1}", largestPartySize, currentPartySize);
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
            PluginLog.LogDebug("Playing sound for {0} commends obtained of a maximum of {1}", commendsObtained,
                               numberOfMatchMadePlayers);

            void Func(Configuration.SubConfiguration config) =>
                SoundEngine.PlaySound(config.GetFilePath(), config.ApplySfxVolume, config.Volume);

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
            WindowSystem.GetWindow(ConfigWindow.Title)!.IsOpen = true;
        }

        private void DrawUserInterface()
        {
            this.WindowSystem.Draw();
        }

        public void DrawConfigWindow()
        {
            WindowSystem.GetWindow(ConfigWindow.Title)!.IsOpen = true;
        }
    }
}
