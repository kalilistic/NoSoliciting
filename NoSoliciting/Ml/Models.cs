using System;

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
        Community,
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
            "COMMUNITY" => MessageCategory.Community,
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
            MessageCategory.Community => "Community ads",
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };

        public static string Description(this MessageCategory category) => category switch {
            MessageCategory.Trade => "Messages advertising trading items or services for gil, such as omnicrafters looking for work or people selling rare items off the market board",
            MessageCategory.FreeCompany => "Advertisements for Free Companies",
            MessageCategory.Normal => "Normal messages that should not be filtered",
            MessageCategory.Phishing => "Messages trying to trick you into revealing your account details in order to steal your account",
            MessageCategory.RmtContent => "Real-money trade involving content (also known as content sellers)",
            MessageCategory.RmtGil => "Real-money trade involving gil or items (also known as RMT bots)",
            MessageCategory.Roleplaying => "Advertisements for personal RP, RP communities, venues, or anything else related to roleplaying",
            MessageCategory.Static => "Statics looking for members or members looking for a static",
            MessageCategory.Community => "Advertisements for general-purpose communities, generally Discord servers",
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };
    }
}
