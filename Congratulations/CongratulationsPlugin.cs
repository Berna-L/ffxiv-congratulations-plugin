using System;
using System.Runtime.InteropServices;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Congratulations.Windows;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Party;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using Lumina.Excel.GeneratedSheets;

namespace Congratulations
{
    public sealed class CongratulationsPlugin : IDalamudPlugin
    {
        public string Name => "Congratulations!";
        private const string CommandName = "/congratsconfig";

        private DalamudPluginInterface PluginInterface { get; init; }
        private CommandManager CommandManager { get; init; }
        private Framework Framework { get; init; }
        private ClientState ClientState { get; init; }
        public Configuration Configuration { get; init; }
        
        public PartyList PartyList { get; set; }

        public readonly WindowSystem WindowSystem = new("Congratulations");

        private short lastCommendationCount = 0;
        private int lastPartySize = 0;

        public CongratulationsPlugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] CommandManager commandManager,
            [RequiredVersion("1.0")] ClientState clientState,
            [RequiredVersion("1.0")] PartyList partyList,
            [RequiredVersion("1.0")] Framework framework)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;
            this.ClientState = clientState;
            this.PartyList = partyList;
            this.Framework = framework;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);
            
            WindowSystem.AddWindow(new ConfigWindow(this));

            this.CommandManager.AddHandler(CommandName, new CommandInfo(OnConfigCommand)
            {
                HelpMessage = "Opens the Congratulations configuration window"
            });
            this.PluginInterface.UiBuilder.Draw += DrawUserInterface;
            this.PluginInterface.UiBuilder.OpenConfigUi += DrawConfigWindow;
            
            this.lastCommendationCount = GetCurrentCommendationCount();
            PluginLog.LogDebug("Starting commendations: {0}", lastCommendationCount);

            lastPartySize = PartyList.Length;
            PluginLog.LogDebug("Starting party size: {0}", lastPartySize);
            ClientState.TerritoryChanged += OnTerritoryChange;
        }
        
        private void OnTerritoryChange(object? sender, ushort @ushort)
        {
            PluginLog.LogDebug("territory changed");
            if (!ClientState.IsLoggedIn) return;
            var currentCommendationCount = GetCurrentCommendationCount();
            var currentPartySize = GetCurrentPartySize();
            if (lastPartySize != currentPartySize)
            {
                PluginLog.Debug("Party size changed from {0} to {1}", lastPartySize, currentPartySize);
            }
            if (currentCommendationCount > lastCommendationCount)
            {
                PluginLog.Debug("Commends changed from {0} to {1}", lastCommendationCount, currentCommendationCount);
                lastCommendationCount = currentCommendationCount;
            }

            lastPartySize = currentPartySize;
        }

        private int GetCurrentPartySize()
        {
            return PartyList.Length;
        }

        private unsafe short GetCurrentCommendationCount()
        {
            // Change this when PlayerState.Instance()->PlayerCommendations offset is updated
            return Marshal.ReadInt16((nint)PlayerState.Instance() + 0x478);
        }


        public void Dispose()
        {
            this.WindowSystem.RemoveAllWindows();
            this.CommandManager.RemoveHandler(CommandName);
            ClientState.TerritoryChanged -= OnTerritoryChange;
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
