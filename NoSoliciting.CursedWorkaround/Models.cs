using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ML.Data;

namespace NoSoliciting.CursedWorkaround {
    public class MessageData {
        private static readonly Regex WardRegex = new Regex(@"w.{0,2}\d", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly Regex PlotRegex = new Regex(@"p.{0,2}\d", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly string[] PlotWords = {
            "plot",
            "apartment",
            "apt",
        };

        private static readonly Regex NumbersRegex = new Regex(@"\d{1,2}.{0,2}\d{1,2}", RegexOptions.Compiled);

        private static readonly string[] TradeWords = {
            "B> ",
            "S> ",
            "buy",
            "sell",
            "WTB",
            "WTS",
        };

        private static readonly Regex SketchUrlRegex = new Regex(@"\.com-\w+\.\w+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public string? Category { get; }

        public uint Channel { get; }

        public string Message { get; }

        public float Weight { get; } = 1;

        public bool PartyFinder => this.Channel == 0;

        public bool Shout => this.Channel == 11 || this.Channel == 30;

        public bool ContainsWard => this.Message.ContainsIgnoreCase("ward") || WardRegex.IsMatch(this.Message);

        public bool ContainsPlot => PlotWords.Any(word => this.Message.ContainsIgnoreCase(word)) || PlotRegex.IsMatch(this.Message);

        public bool ContainsHousingNumbers => NumbersRegex.IsMatch(this.Message);

        public bool ContainsTradeWords => TradeWords.Any(word => this.Message.ContainsIgnoreCase(word));

        public bool ContainsSketchUrl => SketchUrlRegex.IsMatch(this.Message);

        public MessageData(uint channel, string message) {
            this.Channel = channel;
            this.Message = message;
        }
    }

    public class MessagePrediction {
        [ColumnName("PredictedLabel")]
        public string Category { get; set; } = null!;

        [ColumnName("Score")]
        public float[] Probabilities { get; set; } = null!;
    }

    public static class RmtExtensions {
        public static bool ContainsIgnoreCase(this string haystack, string needle) {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
        }
    }
}
