using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace NoSoliciting {
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration {
        [NonSerialized]
        private DalamudPluginInterface pi;

        public int Version { get; set; } = 1;

        public bool FilterChat { get; set; } = true;
        public bool FilterPartyFinder { get; set; } = true;

        public bool AdvancedMode { get; set; } = false;

        public bool CustomChatFilter { get; set; } = false;
        public List<string> ChatSubstrings { get; } = new List<string>();
        public List<string> ChatRegexes { get; } = new List<string>();

        public bool CustomPFFilter { get; set; } = false;
        public List<string> PFSubstrings { get; } = new List<string>();
        public List<string> PFRegexes { get; } = new List<string>();

        public void Initialise(DalamudPluginInterface pi) {
            this.pi = pi ?? throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
        }

        public void Save() {
            this.pi.SavePluginConfig(this);
        }
    }
}
