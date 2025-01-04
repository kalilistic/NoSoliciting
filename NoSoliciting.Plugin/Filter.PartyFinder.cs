using System;
using System.Linq;
using NoSoliciting.Interface;

namespace NoSoliciting {
    public partial class Filter {
        private static class PartyFinder {
            public static bool MatchesCustomFilters(string msg, PluginConfiguration config) {
                if (config == null) {
                    throw new ArgumentNullException(nameof(config), "PluginConfiguration cannot be null");
                }

                if (!config.CustomPFFilter) {
                    return false;
                }

                msg = NoSolUtil.Normalise(msg);

                return config.ValidPfSubstrings.Any(needle => msg.ContainsIgnoreCase(needle))
                    || config.CompiledPfRegexes.Any(needle => needle.IsMatch(msg));
            }
        }
    }
}
