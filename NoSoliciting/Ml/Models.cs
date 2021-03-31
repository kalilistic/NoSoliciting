using System;

namespace NoSoliciting.Ml {
    public enum MessageCategory {
        Trade = 0,
        FreeCompany = 1,
        Normal = 2,
        Phishing = 3,
        RmtContent = 4,
        RmtGil = 5,
        Roleplaying = 6,
        Static = 7,
        StaticSub = 9,
        Community = 8,
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
            "STATIC_SUB" => MessageCategory.StaticSub,
            _ => null,
        };

        #if DEBUG

        public static string ToModelName(this MessageCategory category) => category switch {
            MessageCategory.Trade => "TRADE",
            MessageCategory.FreeCompany => "FC",
            MessageCategory.Normal => "NORMAL",
            MessageCategory.Phishing => "PHISH",
            MessageCategory.RmtContent => "RMT_C",
            MessageCategory.RmtGil => "RMT_G",
            MessageCategory.Roleplaying => "RP",
            MessageCategory.Static => "STATIC",
            MessageCategory.Community => "COMMUNITY",
            MessageCategory.StaticSub => "STATIC_SUB",
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };

        public static MessageCategory? FromName(string? category) => category switch {
            "Trade ads" => MessageCategory.Trade,
            "Free Company ads" => MessageCategory.FreeCompany,
            "Normal messages" => MessageCategory.Normal,
            "Phishing messages" => MessageCategory.Phishing,
            "RMT (content)" => MessageCategory.RmtContent,
            "RMT (gil)" => MessageCategory.RmtGil,
            "Roleplaying ads" => MessageCategory.Roleplaying,
            "Static recruitment" => MessageCategory.Static,
            "Community ads" => MessageCategory.Community,
            "Static substitutes" => MessageCategory.StaticSub,
            _ => null,
        };

        #endif

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
            MessageCategory.StaticSub => "Static substitutes",
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
            MessageCategory.Static => "Statics looking for members or players looking for a static",
            MessageCategory.Community => "Advertisements for general-purpose communities, generally Discord servers",
            MessageCategory.StaticSub => "Statics looking for fill-ins of missing members for clears",
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };
    }
}
