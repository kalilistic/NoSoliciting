using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NoSoliciting {
    public static class RMTUtil {
        private static readonly Dictionary<char, string> replacements = new Dictionary<char, string>() {
            // alphabet
            ['\ue070'] = "?",
            ['\ue071'] = "A",
            ['\ue072'] = "B",
            ['\ue073'] = "C",
            ['\ue074'] = "D",
            ['\ue075'] = "E",
            ['\ue076'] = "F",
            ['\ue077'] = "G",
            ['\ue078'] = "H",
            ['\ue079'] = "I",
            ['\ue07a'] = "J",
            ['\ue07b'] = "K",
            ['\ue07c'] = "L",
            ['\ue07d'] = "M",
            ['\ue07e'] = "N",
            ['\ue07f'] = "O",
            ['\ue080'] = "P",
            ['\ue081'] = "Q",
            ['\ue082'] = "R",
            ['\ue083'] = "S",
            ['\ue084'] = "T",
            ['\ue085'] = "U",
            ['\ue086'] = "V",
            ['\ue087'] = "W",
            ['\ue088'] = "X",
            ['\ue089'] = "Y",
            ['\ue08a'] = "Z",

            // letters in other sets
            ['\ue022'] = "A",
            ['\ue024'] = "_A",
            ['\ue0b0'] = "E",
        };

        public static string Normalise(string input) {
            if (input == null) {
                throw new ArgumentNullException(nameof(input), "input cannot be null");
            }

            foreach (KeyValuePair<char, string> entry in replacements) {
                input = input.Replace($"{entry.Key}", entry.Value);
            }
            return input.Normalize(NormalizationForm.FormKD);
        }
    }
}
