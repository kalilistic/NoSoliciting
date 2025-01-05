using System;
using NoSoliciting.Resources;

namespace NoSoliciting.Ml {
    public enum MessageCategory {
        Normal,
        FreeCompany,
        Phishing,
        Rmt,
        Social,
        Static,
        Trade,
        [Obsolete("Use Static instead.")]
        StaticSub,
        [Obsolete("Use Social instead.")]
        Community,
        [Obsolete("Use Community instead.")]
        Roleplaying,
        [Obsolete("Use Community instead.")]
        Fluff,
        [Obsolete("Use RMT instead.")]
        RmtContent,
        [Obsolete("Use RMT instead.")]
        RmtGil,
    }

    public static class MessageCategoryExt {
        public static readonly MessageCategory[] UiOrder = {
            MessageCategory.Trade,
            MessageCategory.FreeCompany,
            MessageCategory.Phishing,
            MessageCategory.Rmt,
            MessageCategory.Static,
            MessageCategory.Social,
        };

        public static MessageCategory? FromString(string? category) => category switch {
            "TRADE" => MessageCategory.Trade,
            "FC" => MessageCategory.FreeCompany,
            "NORMAL" => MessageCategory.Normal,
            "PHISH" => MessageCategory.Phishing,
            "RMT" => MessageCategory.Rmt,
            "STATIC" => MessageCategory.Static,
            "SOCIAL" => MessageCategory.Social,
            _ => null,
        };

        #if DEBUG
        public static MessageCategory? FromName(string? category) => category switch {
            "Trade ads" => MessageCategory.Trade,
            "Free Company ads" => MessageCategory.FreeCompany,
            "Normal messages" => MessageCategory.Normal,
            "Phishing messages" => MessageCategory.Phishing,
            "RMT (content and gil)" => MessageCategory.Rmt,
            "Static" => MessageCategory.Static,
            "RP & Community ads" => MessageCategory.Social,
            _ => null,
        };
        #endif

        public static string ToModelName(this MessageCategory category) => category switch {
            MessageCategory.Trade => "TRADE",
            MessageCategory.FreeCompany => "FC",
            MessageCategory.Normal => "NORMAL",
            MessageCategory.Phishing => "PHISH",
            MessageCategory.Rmt => "RMT",
            MessageCategory.Static => "STATIC",
            MessageCategory.Social => "SOCIAL",
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };

        public static string Name(this MessageCategory category) => category switch {
            MessageCategory.Trade => Language.TradeCategory,
            MessageCategory.FreeCompany => Language.FreeCompanyCategory,
            MessageCategory.Normal => Language.NormalCategory,
            MessageCategory.Phishing => Language.PhishingCategory,
            MessageCategory.Rmt => Language.RmtCategory,
            MessageCategory.Static => Language.StaticCategory,
            MessageCategory.Social => Language.SocialCategory,
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };

        public static string Description(this MessageCategory category) => category switch {
            MessageCategory.Trade => Language.TradeDescription,
            MessageCategory.FreeCompany => Language.FreeCompanyDescription,
            MessageCategory.Normal => Language.NormalDescription,
            MessageCategory.Phishing => Language.PhishingDescription,
            MessageCategory.Rmt => Language.RmtDescription,
            MessageCategory.Static => Language.StaticDescription,
            MessageCategory.Social => Language.SocialDescription,
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };
    }
}
