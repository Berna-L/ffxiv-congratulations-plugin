using Dalamud.Configuration;
using Dalamud.Plugin;
using System;
using System.IO;

namespace Congratulations
{
    [Serializable]
    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;

        public SubConfiguration OneThird = new("One Third", "one-third.mp3");
        public SubConfiguration TwoThirds = new("Two Thirds", "two-thirds.mp3");
        public SubConfiguration ThreeThirds = new("Three Thirds", "three-thirds.mp3");
        public SubConfiguration AllSevenInAFullParty = new("All seven in a Full Party", "all-seven.mp3");


        public class SubConfiguration
        {
            [NonSerialized]
            public readonly string SectionTitle;

            public bool PlaySound = true;
            public bool UseCustomSound = false;

            [NonSerialized]
            private readonly string defaultFileName;

            public string? CustomFilePath;
            public int Volume = 12;

            public SubConfiguration(string sectionTitle, string defaultFileName)
            {
                this.SectionTitle = sectionTitle;
                this.defaultFileName = defaultFileName;
            }

            public string GetFilePath()
            {
                return UseCustomSound
                           ? CustomFilePath
                           : Path.Combine(
                               Path.GetDirectoryName(Service.PluginInterface.AssemblyLocation.DirectoryName + "\\"),
                               @"Sounds\", defaultFileName);
            }
        }

        // the below exist just to make saving less cumbersome
        [NonSerialized]
        private DalamudPluginInterface? pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
        }

        public void Save()
        {
            this.pluginInterface!.SavePluginConfig(this);
        }
    }
}
