using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoSoliciting {
    public partial class RMTDetection {
        public static class PartyFinder {
            public static bool MatchesCustomFilters(string msg, PluginConfiguration config) {
                if (config == null) {
                    throw new ArgumentNullException(nameof(config), "PluginConfiguration cannot be null");
                }

                if (!config.AdvancedMode || !config.CustomPFFilter) {
                    return false;
                }

                msg = RMTUtil.Normalise(msg);

                return config.PFSubstrings.Any(needle => msg.Contains(needle))
                    || config.PFRegexes.Any(needle => Regex.IsMatch(msg, needle));
            }
        }
    }
}
