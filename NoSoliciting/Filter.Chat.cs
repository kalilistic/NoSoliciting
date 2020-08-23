using System;
using System.Linq;

namespace NoSoliciting {
    public partial class Filter {
        public static class Chat {
            public static bool MatchesCustomFilters(string msg, PluginConfiguration config) {
                if (config == null) {
                    throw new ArgumentNullException(nameof(config), "PluginConfiguration cannot be null");
                }

                if (!config.AdvancedMode || !config.CustomChatFilter) {
                    return false;
                }

                msg = FilterUtil.Normalise(msg);

                return config.ChatSubstrings.Any(needle => msg.ContainsIgnoreCase(needle))
                    || config.CompiledChatRegexes.Any(needle => needle.IsMatch(msg));
            }
        }
    }
}
