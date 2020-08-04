using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace NoSoliciting {
    public partial class RMTDetection {
        public static class PartyFinder {
            private static readonly Regex discordTag = new Regex(@".#\d{4}", RegexOptions.Compiled);
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
                bool containsDiscordTag = discordTag.IsMatch(desc);

                return containsSell && containsDiscordTag && containsContent;
            }
        }
    }
}
