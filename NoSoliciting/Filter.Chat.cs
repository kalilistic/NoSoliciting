using System;
using System.Linq;
using NoSoliciting.Interface;

namespace NoSoliciting {
    public partial class Filter {
        private static class Chat {
            public static bool MatchesCustomFilters(string msg, PluginConfiguration config) {
                if (config == null) {
                    throw new ArgumentNullException(nameof(config), "PluginConfiguration cannot be null");
                }

                if (!config.CustomChatFilter) {
                    return false;
                }

                msg = NoSolUtil.Normalise(msg);

                return config.ChatSubstrings.Any(needle => msg.ContainsIgnoreCase(needle))
                    || config.CompiledChatRegexes.Any(needle => needle.IsMatch(msg));
            }
        }
    }
}
