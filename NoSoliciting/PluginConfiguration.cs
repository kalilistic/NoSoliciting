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

        private DalamudPluginInterface Interface { get; set; } = null!;

        public int Version { get; set; } = 2;

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
        public List<Regex> CompiledPfRegexes { get; private set; } = new();

        public bool FilterHugeItemLevelPFs { get; set; }

        public bool FollowGameLanguage { get; set; }

        public HashSet<MessageCategory> BasicMlFilters { get; set; } = new() {
            MessageCategory.RmtGil,
            MessageCategory.RmtContent,
            MessageCategory.Phishing,
        };

        public Dictionary<MessageCategory, HashSet<ChatType>> MlFilters { get; set; } = new() {
            [MessageCategory.RmtGil] = new HashSet<ChatType> {
                ChatType.Say,
                ChatType.Shout,
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
            [MessageCategory.StaticSub] = new HashSet<ChatType> {
                ChatType.None,
            },
            [MessageCategory.Trade] = new HashSet<ChatType> {
                ChatType.None,
            },
            [MessageCategory.Community] = new HashSet<ChatType> {
                ChatType.None,
            },
            [MessageCategory.Fluff] = new HashSet<ChatType> {
                ChatType.None,
            },
        };

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

        internal bool MlEnabledOn(MessageCategory category, ChatType chatType) {
            HashSet<ChatType>? filtered;

            if (this.AdvancedMode) {
                if (!this.MlFilters.TryGetValue(category, out filtered)) {
                    return false;
                }
            } else {
                // check to see if the user has this category filtered
                if (!this.BasicMlFilters.Contains(category)) {
                    return false;
                }

                // get the chat types that this category is enabled on by default
                if (!Default.MlFilters.TryGetValue(category, out filtered)) {
                    return false;
                }
            }

            return filtered.Contains(chatType);
        }

        internal IEnumerable<MessageCategory> CreateFiltersClone() {
            var filters = new HashSet<MessageCategory>();

            foreach (var category in (MessageCategory[]) Enum.GetValues(typeof(MessageCategory))) {
                if (this.AdvancedMode) {
                    if (this.MlFilters.TryGetValue(category, out var filtered) && filtered.Count > 0) {
                        filters.Add(category);
                    }
                } else {
                    if (this.BasicMlFilters.Contains(category)) {
                        filters.Add(category);
                    }
                }
            }

            return filters;
        }
    }
}
