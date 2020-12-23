using System;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.ML.Data;

namespace NoSoliciting.Ml {
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
            MessageCategory.Trade => "Trade ads",
            MessageCategory.FreeCompany => "Free Company ads",
            MessageCategory.Normal => "Normal messages",
            MessageCategory.Phishing => "Phishing messages",
            MessageCategory.RmtContent => "RMT (content)",
            MessageCategory.RmtGil => "RMT (gil)",
            MessageCategory.Roleplaying => "Roleplaying ads",
            MessageCategory.Static => "Static recruitment",
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };
    }
}
