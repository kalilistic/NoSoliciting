using System;
using System.Linq;

namespace NoSoliciting {
    public partial class Filter {
        public static class PartyFinder {
            public static bool MatchesCustomFilters(string msg, PluginConfiguration config) {
                if (config == null) {
                    throw new ArgumentNullException(nameof(config), "PluginConfiguration cannot be null");
                }

                if (!config.CustomPFFilter) {
                    return false;
                }

                msg = FilterUtil.Normalise(msg);

                return config.PFSubstrings.Any(needle => msg.ContainsIgnoreCase(needle))
                    || config.CompiledPFRegexes.Any(needle => needle.IsMatch(msg));
            }
        }
    }
}
