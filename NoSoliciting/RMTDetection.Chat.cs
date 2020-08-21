using Dalamud.Game.Chat;
using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoSoliciting {
    public partial class RMTDetection {
        public static class Chat {
            public static bool MatchesCustomFilters(string msg, PluginConfiguration config) {
                if (config == null) {
                    throw new ArgumentNullException(nameof(config), "PluginConfiguration cannot be null");
                }

                if (!config.AdvancedMode || !config.CustomChatFilter) {
                    return false;
                }

                msg = RMTUtil.Normalise(msg);

                return config.ChatSubstrings.Any(needle => msg.Contains(needle))
                    || config.ChatRegexes.Any(needle => Regex.IsMatch(msg, needle));
            }
        }
    }
}
