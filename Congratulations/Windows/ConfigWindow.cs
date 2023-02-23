using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Components;
using Dalamud.Interface.ImGuiFileDialog;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Congratulations.Windows;

public class ConfigWindow : Window, IDisposable
{
    public static readonly String Title = "Congratulations Configuration";

    private readonly Configuration configuration;

    private readonly FileDialogManager dialogManager;

    public ConfigWindow(CongratulationsPlugin congratulationsPlugin) : base(Title, ImGuiWindowFlags.NoCollapse)
    {
        this.Size = new Vector2(500, 500);
        this.SizeCondition = ImGuiCond.Appearing;

        this.configuration = congratulationsPlugin.Configuration;
        dialogManager = new FileDialogManager
            { AddedWindowFlags = ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoDocking };
        dialogManager.CustomSideBarItems.Add((Environment.ExpandEnvironmentVariables("%USERNAME%"),
                                                 Environment.ExpandEnvironmentVariables("%USERPROFILE%"),
                                                 FontAwesomeIcon.User, 0));
    }

    public override void Draw()
    {
        DrawSection(configuration.OneThird);
        DrawSection(configuration.TwoThirds);
        DrawSection(configuration.ThreeThirds);
        DrawSection(configuration.AllSevenInAFullParty);
        dialogManager.Draw();
    }

    private void DrawSection(Configuration.SubConfiguration config)
    {
        if (!ImGui.TreeNode(config.SectionTitle)) return;
        var playSound = config.PlaySound;
        if (ImGui.Checkbox("Play sound", ref playSound))
        {
            config.PlaySound = playSound;
            configuration.Save();
        }

        if (config.PlaySound)
        {
            var volume = config.Volume;
            if (ImGui.SliderInt("Volume", ref volume, 0, 100))
            {
                config.Volume = volume;
                configuration.Save();
            }

            ImGui.SameLine();
            if (ImGuiComponents.IconButton(FontAwesomeIcon.Play))
            {
                SoundEngine.PlaySound(config.GetFilePath(), config.ApplySfxVolume, config.Volume * 0.01f);
            }
            
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Preview sound on current volume");
            }
            var applySfxVolume = config.ApplySfxVolume;
            if (ImGui.Checkbox("Affected by the game's sound effects volume", ref applySfxVolume))
            {
                config.ApplySfxVolume = applySfxVolume;
                configuration.Save();
            }
            ImGui.SameLine();
            ImGuiComponents.HelpMarker("If enabled, consider the volume set here to be in relation to the game's other SFX," +
                                       "\nsince the effective volume will also vary with your Master and Sound Effects volume." +
                                       "\nIf disabled, It'll always play at the set volume, even if the game is muted internally.");

            var useCustomSound = config.UseCustomSound;
            if (ImGui.Checkbox("Use custom sound", ref useCustomSound))
            {
                config.UseCustomSound = useCustomSound;
                configuration.Save();
            }

            if (config.UseCustomSound)
            {
                var path = config.CustomFilePath ?? "";
                ImGui.InputText("", ref path, 512, ImGuiInputTextFlags.ReadOnly);
                ImGui.SameLine();


                void UpdatePath(bool success, List<string> paths)
                {
                    if (success && paths.Count > 0)
                    {
                        config.CustomFilePath = paths[0];
                        configuration.Save();
                    }
                }

                if (ImGuiComponents.IconButton(FontAwesomeIcon.Folder))
                {
                    dialogManager.OpenFileDialog("Select the file", "Audio files{.wav,.mp3}", UpdatePath, 1,
                                                 config.CustomFilePath ??
                                                 Environment.ExpandEnvironmentVariables("%USERPROFILE%"));
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Open file browser...");
                }
            }
        }

        ImGui.TreePop();
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
