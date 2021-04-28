using System;
using CheapLoc;
using Dalamud;

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
            MessageCategory.Trade => Loc.Localize("TradeCategory", "Trade ads"),
            MessageCategory.FreeCompany => Loc.Localize("FreeCompanyCategory", "Free Company ads"),
            MessageCategory.Normal => Loc.Localize("NormalCategory", "Normal messages"),
            MessageCategory.Phishing => Loc.Localize("PhishingCategory", "Phishing messages"),
            MessageCategory.RmtContent => Loc.Localize("RmtContentCategory", "RMT (content)"),
            MessageCategory.RmtGil => Loc.Localize("RmtGilCategory", "RMT (gil)"),
            MessageCategory.Roleplaying => Loc.Localize("RoleplayingCategory", "Roleplaying ads"),
            MessageCategory.Static => Loc.Localize("StaticCategory", "Static recruitment"),
            MessageCategory.Community => Loc.Localize("CommunityCategory", "Community ads"),
            MessageCategory.StaticSub => Loc.Localize("StaticSubCategory", "Static substitutes"),
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };

        public static string Description(this MessageCategory category) => category switch {
            MessageCategory.Trade => Loc.Localize("TradeDescription", "Messages advertising trading items or services for gil, such as omnicrafters looking for work or people selling rare items off the market board"),
            MessageCategory.FreeCompany => Loc.Localize("FreeCompanyDescription", "Advertisements for Free Companies"),
            MessageCategory.Normal => Loc.Localize("NormalDescription", "Normal messages that should not be filtered"),
            MessageCategory.Phishing => Loc.Localize("PhishingDescription", "Messages trying to trick you into revealing your account details in order to steal your account"),
            MessageCategory.RmtContent => Loc.Localize("RmtContentDescription", "Real-money trade involving content (also known as content sellers)"),
            MessageCategory.RmtGil => Loc.Localize("RmtGilDescription", "Real-money trade involving gil or items (also known as RMT bots)"),
            MessageCategory.Roleplaying => Loc.Localize("RoleplayingDescription", "Advertisements for personal RP, RP communities, venues, or anything else related to roleplaying"),
            MessageCategory.Static => Loc.Localize("StaticDescription", "Statics looking for members or players looking for a static"),
            MessageCategory.Community => Loc.Localize("CommunityDescription", "Advertisements for general-purpose communities, generally Discord servers"),
            MessageCategory.StaticSub => Loc.Localize("StaticSubDescription", "Statics looking for fill-ins of missing members for clears"),
            _ => throw new ArgumentException("Invalid category", nameof(category)),
        };
    }
}
