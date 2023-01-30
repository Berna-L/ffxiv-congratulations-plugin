using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.IO;
using Dalamud.Logging;

namespace Congratulations
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public SubConfiguration OneThird;
        public SubConfiguration TwoThirds;
        public SubConfiguration ThreeThirds;
        public SubConfiguration AllSevenInAFullParty;

        public class SubConfiguration
        {
            [NonSerialized]
            public readonly string SectionTitle;

            [NonSerialized]
            private readonly DalamudPluginInterface pluginInterface;
            public bool PlaySound = true;
            public bool UseCustomSound = false;

            [NonSerialized]
            private readonly string defaultFileName;
            public string? CustomFilePath;
            public int Volume = 12;
            
            public SubConfiguration(string sectionTitle, DalamudPluginInterface pluginInterface, string defaultFileName)
            {
                this.SectionTitle = sectionTitle;
                this.pluginInterface = pluginInterface;
                this.defaultFileName = defaultFileName;
            }

            public string getFilePath()
            {
                return UseCustomSound ? CustomFilePath : Path.Combine(Path.GetDirectoryName(pluginInterface.AssemblyLocation.DirectoryName + "\\"), @"Sounds\", defaultFileName);
            }
        }
        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            this.OneThird = new SubConfiguration("One Third", pluginInterface, "one-third.mp3");
            TwoThirds = new SubConfiguration("Two Thirds", pluginInterface, "two-thirds.mp3");
            ThreeThirds = new SubConfiguration("Three Thirds", pluginInterface, "three-thirds.mp3");
            AllSevenInAFullParty = new SubConfiguration("All seven in a Full Party", pluginInterface, "all-seven.mp3");
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
