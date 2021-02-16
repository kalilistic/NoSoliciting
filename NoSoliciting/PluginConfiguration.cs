using Dalamud.Configuration;
using Dalamud.Plugin;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using NoSoliciting.Ml;

namespace NoSoliciting {
    [Serializable]
    public class PluginConfiguration : IPluginConfiguration {
        public static readonly PluginConfiguration Default = new();

        [NonSerialized]
        private DalamudPluginInterface pi;

        public int Version { get; set; } = 1;

        [Obsolete("Use FilterStatus")]
        public bool FilterChat { get; set; } = true;

        [Obsolete("Use FilterStatus")]
        public bool FilterFCRecruitments { get; set; } = false;

        [Obsolete("Use FilterStatus")]
        public bool FilterChatRPAds { get; set; } = false;

        [Obsolete("Use FilterStatus")]
        public bool FilterPartyFinder { get; set; } = true;

        [Obsolete("Use FilterStatus")]
        public bool FilterPartyFinderRPAds { get; set; } = false;

        public Dictionary<string, bool> FilterStatus { get; private set; } = new();

        public bool AdvancedMode { get; set; }

        public bool CustomChatFilter { get; set; }
        public List<string> ChatSubstrings { get; } = new();
        public List<string> ChatRegexes { get; } = new();

        [JsonIgnore]
        public List<Regex> CompiledChatRegexes { get; private set; } = new();

        public bool CustomPFFilter { get; set; }
        public List<string> PFSubstrings { get; } = new();
        public List<string> PFRegexes { get; } = new();

        [JsonIgnore]
        public List<Regex> CompiledPFRegexes { get; private set; } = new();

        public bool FilterHugeItemLevelPFs { get; set; }

        public bool UseMachineLearning { get; set; }

        public HashSet<MessageCategory> BasicMlFilters { get; set; } = new() {
            MessageCategory.RmtGil,
            MessageCategory.RmtContent,
            MessageCategory.Phishing,
        };
        public Dictionary<MessageCategory, HashSet<ChatType>> MlFilters { get; set; } = new() {
            [MessageCategory.RmtGil] = new HashSet<ChatType> {
                ChatType.Say,
            },
            [MessageCategory.RmtContent] = new HashSet<ChatType> {
                ChatType.None,
            },
            [MessageCategory.Phishing] = new HashSet<ChatType> {
                ChatType.TellIncoming,
            },
            [MessageCategory.Roleplaying] = new HashSet<ChatType> {
                ChatType.None,
                ChatType.Shout,
                ChatType.Yell,
            },
            [MessageCategory.FreeCompany] = new HashSet<ChatType> {
                ChatType.None,
                ChatType.Shout,
                ChatType.Yell,
                ChatType.TellIncoming,
            },
            [MessageCategory.Static] = new HashSet<ChatType> {
                ChatType.None,
            },
            [MessageCategory.Trade] = new HashSet<ChatType> {
                ChatType.None,
            },
        };

        public bool LogFilteredPfs { get; set; } = true;
        public bool LogFilteredChat { get; set; } = true;

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

        internal bool MlEnabledOn(MessageCategory category, ChatType chatType) {
            HashSet<ChatType> filtered;

            if (this.AdvancedMode) {
                if (!this.MlFilters.TryGetValue(category, out filtered)) {
                    return false;
                }
            } else {
                if (!this.BasicMlFilters.Contains(category)) {
                    return false;
                }

                if (!Default.MlFilters.TryGetValue(category, out filtered)) {
                    return false;
                }
            }

            return filtered.Contains(chatType);
        }
    }
}
