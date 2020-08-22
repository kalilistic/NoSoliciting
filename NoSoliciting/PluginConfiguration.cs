using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoSoliciting {
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration {
        [NonSerialized]
        private DalamudPluginInterface pi;

        public int Version { get; set; } = 1;

        [Obsolete("Use EnabledFilters")]
        public bool FilterChat { get; set; } = true;
        [Obsolete("Use EnabledFilters")]
        public bool FilterFCRecruitments { get; set; } = false;
        [Obsolete("Use EnabledFilters")]
        public bool FilterChatRPAds { get; set; } = false;

        [Obsolete("Use EnabledFilters")]
        public bool FilterPartyFinder { get; set; } = true;
        [Obsolete("Use EnabledFilters")]
        public bool FilterPartyFinderRPAds { get; set; } = false;

        public Dictionary<string, bool> FilterStatus { get; private set; } = new Dictionary<string, bool>();

        public bool AdvancedMode { get; set; } = false;

        public bool CustomChatFilter { get; set; } = false;
        public List<string> ChatSubstrings { get; } = new List<string>();
        public List<string> ChatRegexes { get; } = new List<string>();
        [JsonIgnore]
        public List<Regex> CompiledChatRegexes { get; private set; } = new List<Regex>();

        public bool CustomPFFilter { get; set; } = false;
        public List<string> PFSubstrings { get; } = new List<string>();
        public List<string> PFRegexes { get; } = new List<string>();
        [JsonIgnore]
        public List<Regex> CompiledPFRegexes { get; private set; } = new List<Regex>();

        public void Initialise(DalamudPluginInterface pi) {
            this.pi = pi ?? throw new ArgumentNullException(nameof(pi), "DalamudPluginInterface cannot be null");
            this.CompileRegexes();
        }

        public void Save() {
            this.pi.SavePluginConfig(this);
        }

        public void CompileRegexes() {
            this.CompiledChatRegexes = this.ChatRegexes
                .Select(reg => new Regex(reg, RegexOptions.Compiled))
                .ToList();
            this.CompiledPFRegexes = this.PFRegexes
                .Select(reg => new Regex(reg, RegexOptions.Compiled))
                .ToList();
        }
    }
}
