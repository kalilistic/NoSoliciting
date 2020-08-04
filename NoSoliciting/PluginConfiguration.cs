using Dalamud.Configuration;
using Dalamud.Plugin;
using System;

namespace NoSoliciting {
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration {
        [NonSerialized]
        private readonly DalamudPluginInterface pi;

        public int Version { get; set; } = 1;

        public bool FilterChat { get; set; } = true;
        public bool FilterPartyFinder { get; set; } = true;

        public PluginConfiguration(DalamudPluginInterface pi) {
            this.pi = pi ?? throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
        }

        public void Save() {
            this.pi.SavePluginConfig(this);
        }
    }
}
