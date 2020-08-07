using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoSoliciting {
    public partial class RMTDetection {
        public static class PartyFinder {
            private static readonly Regex[] discord = {
                new Regex(@".#\d{4}", RegexOptions.Compiled),
                new Regex(@"https://discord\.(gg|io)/\w+", RegexOptions.Compiled),
            };
            private static readonly string[] content = {
                "eden",
                "savage",
                "primal",
                "ultimate",
            };
            private static readonly string[] selling = {
                "sell",
                "$ell",
                "sale",
                "price",
                "cheap",
            };

            public static bool IsRMT(string desc) {
                if (desc == null) {
                    throw new ArgumentNullException(nameof(desc), "description string cannot be null");
                }

                desc = RMTUtil.Normalise(desc).ToLowerInvariant();

                bool containsSell = selling.Any(needle => desc.Contains(needle));
                bool containsContent = content.Any(needle => desc.Contains(needle));
                bool containsDiscord = discord.Any(needle => needle.IsMatch(desc));

                return containsSell && containsDiscord && containsContent;
            }

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
