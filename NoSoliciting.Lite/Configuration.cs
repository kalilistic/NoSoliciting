using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;

namespace NoSoliciting.Lite {
    [Serializable]
    internal class Configuration : IPluginConfiguration {
        private DalamudPluginInterface Interface { get; set; } = null!;

        public int Version { get; set; } = 1;

        public bool CustomChatFilter { get; set; }
        public List<string> ChatSubstrings { get; } = new();
        public List<string> ChatRegexes { get; } = new();

        [JsonIgnore]
        public List<Regex> CompiledChatRegexes { get; private set; } = new();

        public bool CustomPFFilter { get; set; }
        public List<string> PFSubstrings { get; } = new();
        public List<string> PFRegexes { get; } = new();

        [JsonIgnore]
        public List<Regex> CompiledPfRegexes { get; private set; } = new();

        public bool LogFilteredPfs { get; set; } = true;
        public bool LogFilteredChat { get; set; } = true;

        public bool ConsiderPrivatePfs { get; set; }

        public IEnumerable<string> ValidChatSubstrings => this.ChatSubstrings.Where(needle => !string.IsNullOrWhiteSpace(needle));
        public IEnumerable<string> ValidPfSubstrings => this.PFSubstrings.Where(needle => !string.IsNullOrWhiteSpace(needle));

        public void Initialise(DalamudPluginInterface pi) {
            this.Interface = pi;
            this.CompileRegexes();
        }

        public void Save() {
            this.Interface.SavePluginConfig(this);
        }

        public void CompileRegexes() {
            this.CompiledChatRegexes = this.ChatRegexes
                .Where(reg => !string.IsNullOrWhiteSpace(reg))
                .Select(reg => new Regex(reg, RegexOptions.Compiled))
                .ToList();
            this.CompiledPfRegexes = this.PFRegexes
                .Where(reg => !string.IsNullOrWhiteSpace(reg))
                .Select(reg => new Regex(reg, RegexOptions.Compiled))
                .ToList();
        }
    }
}
