using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;

namespace NoSoliciting {
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration {
        [NonSerialized]
        private DalamudPluginInterface pi;

        public int Version { get; set; } = 1;

        public bool FilterChat { get; set; } = true;
        public bool FilterPartyFinder { get; set; } = true;

        public void Initialise(DalamudPluginInterface pi) {
            this.pi = pi ?? throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
        }

        public void Save() {
            this.pi.SavePluginConfig(this);
        }
    }
}
