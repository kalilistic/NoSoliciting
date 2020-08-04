using System.Linq;
using System.Text.RegularExpressions;

namespace NoSoliciting {
    public partial class RMTDetection {
        public static class Chat {
            private static readonly string[] rmtSubstrings = {
                "4KGOLD",
                "We have sufficient stock",
                "VPK.OM",
                "Gil for free",
                "www.so9.com",
                "Fast & Convenient",
                "Cheap & Safety Guarantee",
                "【Code|A O A U E",
                "igfans",
                "4KGOLD.COM",
                "Cheapest Gil with",
                "pvp and bank on google",
                "Selling Cheap GIL",
                "ff14mogstation.com",
                "Cheap Gil 1000k",
                "gilsforyou",
                "server 1000K =",
                "gils_selling",
                "E A S Y.C O M",
                "bonus code",
                "mins delivery guarantee",
                "Sell cheap",
                "Salegm.com",
                "cheap Mog",
                "Off Code:",
                "FF14Mog.com",
                "使用する5％オ",
                "offers Fantasia",
            };
            private static readonly Regex rmtRegex = new Regex(@"Off Code( *)", RegexOptions.Compiled);

            public static bool IsRMT(string msg) {
                msg = RMTUtil.Normalise(msg);

                return rmtSubstrings.Any(needle => msg.Contains(needle))
                    || rmtRegex.IsMatch(msg);
            }
        }
    }
}
