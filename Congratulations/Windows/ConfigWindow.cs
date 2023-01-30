using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using Dalamud.Logging;
using ImGuiNET;

namespace Congratulations.Windows;

public class ConfigWindow : Window, IDisposable
{
    public static readonly String Title = "Congratulations Configuration";
    
    private readonly Configuration configuration;

    private readonly FileDialogManager dialogManager;

    public ConfigWindow(CongratulationsPlugin congratulationsPlugin) : base(
        Title,
        ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
        ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(500, 500);
        this.SizeCondition = ImGuiCond.Appearing;

        this.configuration = congratulationsPlugin.Configuration;
        dialogManager = new FileDialogManager { AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking};
        dialogManager.CustomSideBarItems.Add((Environment.ExpandEnvironmentVariables("%USERNAME%"), Environment.ExpandEnvironmentVariables("%USERPROFILE%"), FontAwesomeIcon.User, 0));
    }
    
    public override void Draw()
    {
        dialogManager.Draw();

        if (ImGui.Button("Save"))
        {
            configuration.Save();
        }
    }
    
    public override void OnClose()
    {
        dialogManager.Reset();
    }
    
    

    public void Dispose()
    {
        dialogManager.Reset();
    }
}
