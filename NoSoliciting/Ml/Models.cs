using System;
using NoSoliciting.Resources;

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
        StaticSub,
    }

    public static class MessageCategoryExt {
        public static readonly MessageCategory[] UiOrder = {
            MessageCategory.Trade,
            MessageCategory.FreeCompany,
            MessageCategory.Phishing,
            MessageCategory.RmtContent,
            MessageCategory.RmtGil,
            MessageCategory.Roleplaying,
            MessageCategory.Static,
            MessageCategory.StaticSub,
            MessageCategory.Community,
        };

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
            MessageCategory.Trade => Language.TradeCategory,
            MessageCategory.FreeCompany => Language.FreeCompanyCategory,
            MessageCategory.Normal => Language.NormalCategory,
            MessageCategory.Phishing => Language.PhishingCategory,
            MessageCategory.RmtContent => Language.RmtContentCategory,
            MessageCategory.RmtGil => Language.RmtGilCategory,
            MessageCategory.Roleplaying => Language.RoleplayingCategory,
            MessageCategory.Static => Language.StaticCategory,
            MessageCategory.Community => Language.CommunityCategory,
            MessageCategory.StaticSub => Language.StaticSubCategory,
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };

        public static string Description(this MessageCategory category) => category switch {
            MessageCategory.Trade => Language.TradeDescription,
            MessageCategory.FreeCompany => Language.FreeCompanyDescription,
            MessageCategory.Normal => Language.NormalDescription,
            MessageCategory.Phishing => Language.PhishingDescription,
            MessageCategory.RmtContent => Language.RmtContentDescription,
            MessageCategory.RmtGil => Language.RmtGilDescription,
            MessageCategory.Roleplaying => Language.RoleplayingDescription,
            MessageCategory.Static => Language.StaticDescription,
            MessageCategory.Community => Language.CommunityDescription,
            MessageCategory.StaticSub => Language.StaticSubDescription,
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };
    }
}
