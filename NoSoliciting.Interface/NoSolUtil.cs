using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NoSoliciting.Interface {
    public static class NoSolUtil {
        private static readonly Dictionary<char, string> Replacements = new() {
            // numerals
            ['\ue055'] = "1",
            ['\ue056'] = "2",
            ['\ue057'] = "3",
            ['\ue058'] = "4",
            ['\ue059'] = "5",
            ['\ue099'] = "10",
            ['\ue09a'] = "11",
            ['\ue09b'] = "12",
            ['\ue09c'] = "13",
            ['\ue09d'] = "14",
            ['\ue09e'] = "15",
            ['\ue09f'] = "16",
            ['\ue0a0'] = "17",
            ['\ue0a1'] = "18",
            ['\ue0a2'] = "19",
            ['\ue0a3'] = "20",
            ['\ue0a4'] = "21",
            ['\ue0a5'] = "22",
            ['\ue0a6'] = "23",
            ['\ue0a7'] = "24",
            ['\ue0a8'] = "25",
            ['\ue0a9'] = "26",
            ['\ue0aa'] = "27",
            ['\ue0ab'] = "28",
            ['\ue0ac'] = "29",
            ['\ue0ad'] = "30",
            ['\ue0ae'] = "31",

            // symbols
            ['\ue0af'] = "+",
            ['\ue070'] = "?",

            // letters in other sets
            ['\ue022'] = "A",
            ['\ue024'] = "_A",
            ['\ue0b0'] = "E",
        };

        private const char LowestReplacement = '\ue022';

        private static readonly char[] SpaceSymbols = {
            '/', '|',
            '(', ')',
            '[', ']',
            '{', '}',
            '<', '>',
            '=', '+',
            '.', ',', '?', '!',
            '~', '-',
        };

        public static string Spacify(this string input) {
            return SpaceSymbols.Aggregate(input, (current, sym) => current.Replace(sym, ' '));
        }

        public static string Normalise(string input, bool spacify = false) {
            if (input == null) {
                throw new ArgumentNullException(nameof(input), "input cannot be null");
            }

            // replace ffxiv private use chars
            var builder = new StringBuilder(input.Length);
            foreach (var c in input) {
                if (c < LowestReplacement) {
                    goto AppendNormal;
                }

                // alphabet
                if (c >= 0xe071 && c <= 0xe08a) {
                    builder.Append((char) (c - 0xe030));
                    continue;
                }

                // 0 to 9
                if (c >= 0xe060 && c <= 0xe069) {
                    builder.Append((char) (c - 0xe030));
                    continue;
                }

                // 1 to 9
                if (c >= 0xe0b1 && c <= 0xe0b9) {
                    builder.Append((char) (c - 0xe080));
                    continue;
                }

                // 1 to 9 again
                if (c >= 0xe090 && c <= 0xe098) {
                    builder.Append((char) (c - 0xe05f));
                    continue;
                }

                // replacements in map
                if (Replacements.TryGetValue(c, out var rep)) {
                    builder.Append(rep);
                    continue;
                }

                AppendNormal:
                builder.Append(c);
            }

            input = builder.ToString();

            // NFKD unicode normalisation
            var normalised = input.Normalize(NormalizationForm.FormKD);

            // replace several symbols with spaces instead
            return spacify ? normalised.Spacify() : normalised;
        }
    }
}
