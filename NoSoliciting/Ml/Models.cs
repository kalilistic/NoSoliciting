using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ML.Data;

namespace NoSoliciting.Ml {
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
            "B>",
            "S>",
            "buy",
            "sell",
        };

        public string? Category { get; }

        public uint Channel { get; }

        public string Message { get; }

        public bool PartyFinder => this.Channel == 0;

        public bool Shout => this.Channel == 11 || this.Channel == 30;

        public bool ContainsWard => this.Message.ContainsIgnoreCase("ward") || WardRegex.IsMatch(this.Message);

        public bool ContainsPlot => PlotWords.Any(word => this.Message.ContainsIgnoreCase(word)) || PlotRegex.IsMatch(this.Message);

        public bool ContainsHousingNumbers => NumbersRegex.IsMatch(this.Message);

        public bool ContainsTradeWords => TradeWords.Any(word => this.Message.ContainsIgnoreCase(word));

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

    public enum MessageCategory {
        Trade,
        FreeCompany,
        Normal,
        Phishing,
        RmtContent,
        RmtGil,
        Roleplaying,
        Static,
    }

    public static class MessageCategoryExt {
        public static MessageCategory? FromString(string? category) => category switch {
            "TRADE" => MessageCategory.Trade,
            "FC" => MessageCategory.FreeCompany,
            "NORMAL" => MessageCategory.Normal,
            "PHISH" => MessageCategory.Phishing,
            "RMT_C" => MessageCategory.RmtContent,
            "RMT_G" => MessageCategory.RmtGil,
            "RP" => MessageCategory.Roleplaying,
            "STATIC" => MessageCategory.Static,
            _ => null,
        };

        public static string Name(this MessageCategory category) => category switch {
            MessageCategory.Trade => "Trades",
            MessageCategory.FreeCompany => "Free Company ads",
            MessageCategory.Normal => "Normal messages",
            MessageCategory.Phishing => "Phishing messages",
            MessageCategory.RmtContent => "RMT (content)",
            MessageCategory.RmtGil => "RMT (gil)",
            MessageCategory.Roleplaying => "Roleplaying",
            MessageCategory.Static => "Static recruitment",
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };
    }
}
