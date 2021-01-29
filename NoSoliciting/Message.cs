using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;

namespace NoSoliciting {
    public class Message {
        public Guid Id { get; }
        public uint DefinitionsVersion { get; }
        public DateTime Timestamp { get; }
        public ChatType ChatType { get; }
        public SeString Sender { get; }
        public SeString Content { get; }
        public bool Ml { get; }
        public string? FilterReason { get; }

        public Message(uint defsVersion, ChatType type, SeString sender, SeString content, bool ml, string? reason) {
            this.Id = Guid.NewGuid();
            this.DefinitionsVersion = defsVersion;
            this.Timestamp = DateTime.Now;
            this.ChatType = type;
            this.Sender = sender;
            this.Content = content;
            this.Ml = ml;
            this.FilterReason = reason;
        }

        public Message(uint defsVersion, ChatType type, string sender, string content, bool ml, string? reason) : this(
            defsVersion,
            type,
            new SeString(new Payload[] {new TextPayload(sender)}),
            new SeString(new Payload[] {new TextPayload(content)}),
            ml,
            reason
        ) {
        }

        [Serializable]
        [JsonObject(NamingStrategyType = typeof(SnakeCaseNamingStrategy))]
        private class JsonMessage {
            public Guid Id { get; set; }
            public uint DefinitionsVersion { get; set; }
            public DateTime Timestamp { get; set; }

            public ushort Type { get; set; }

            // note: cannot use byte[] because Newtonsoft thinks it's a good idea to always base64 byte[]
            //       and I don't want to write a custom converter to overwrite their stupiditiy
            public List<byte> Sender { get; set; }
            public List<byte> Content { get; set; }
            public string? Reason { get; set; }
        }

        public string ToJson() {
            var msg = new JsonMessage {
                Id = this.Id,
                DefinitionsVersion = this.DefinitionsVersion,
                Timestamp = this.Timestamp,
                Type = (ushort) this.ChatType,
                Sender = this.Sender.Encode().ToList(),
                Content = this.Content.Encode().ToList(),
                Reason = this.FilterReason,
            };

            return JsonConvert.SerializeObject(msg, new JsonSerializerSettings {
                TypeNameHandling = TypeNameHandling.None,
            });
        }
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32")]
    public enum ChatType : ushort {
        None = 0,
        Debug = 1,
        Urgent = 2,
        Notice = 3,
        Say = 10,
        Shout = 11,
        TellOutgoing = 12,
        TellIncoming = 13,
        Party = 14,
        Alliance = 15,
        Linkshell1 = 16,
        Linkshell2 = 17,
        Linkshell3 = 18,
        Linkshell4 = 19,
        Linkshell5 = 20,
        Linkshell6 = 21,
        Linkshell7 = 22,
        Linkshell8 = 23,
        FreeCompany = 24,
        NoviceNetwork = 27,
        CustomEmote = 28,
        StandardEmote = 29,
        Yell = 30,
        CrossParty = 32,
        PvpTeam = 36,
        CrossLinkshell1 = 37,
        Damage = 41,
        Miss = 42,
        Action = 43,
        Item = 44,
        Healing = 45,
        GainBuff = 46,
        GainDebuff = 47,
        LoseBuff = 48,
        LoseDebuff = 49,
        Alarm = 55,
        Echo = 56,
        System = 57,
        BattleSystem = 58,
        GatheringSystem = 59,
        Error = 60,
        NpcDialogue = 61,
        LootNotice = 62,
        Progress = 64,
        LootRoll = 65,
        Crafting = 66,
        Gathering = 67,
        NpcAnnouncement = 68,
        FreeCompanyAnnouncement = 69,
        FreeCompanyLoginLogout = 70,
        RetainerSale = 71,
        PeriodicRecruitmentNotification = 72,
        Sign = 73,
        RandomNumber = 74,
        NoviceNetworkSystem = 75,
        Orchestrion = 76,
        PvpTeamAnnouncement = 77,
        PvpTeamLoginLogout = 78,
        MessageBook = 79,
        GmTell = 80,
        GmSay = 81,
        GmShout = 82,
        GmYell = 83,
        GmParty = 84,
        GmFreeComapny = 85,
        GmLinkshell1 = 86,
        GmLinkshell2 = 87,
        GmLinkshell3 = 88,
        GmLinkshell4 = 89,
        GmLinkshell5 = 90,
        GmLinkshell6 = 91,
        GmLinkshell7 = 92,
        GmLinkshell8 = 93,
        GmNoviceNetwork = 94,
        CrossLinkshell2 = 101,
        CrossLinkshell3 = 102,
        CrossLinkshell4 = 103,
        CrossLinkshell5 = 104,
        CrossLinkshell6 = 105,
        CrossLinkshell7 = 106,
        CrossLinkshell8 = 107,
    }

    public static class ChatTypeExt {
        private const ushort Clear7 = ~(~0 << 7);

        public static byte LogKind(this ChatType type) => type switch {
            ChatType.TellIncoming => (byte) ChatType.TellOutgoing,
            _ => (byte) type,
        };

        public static string Name(this ChatType type, DataManager data) {
            switch (type) {
                case ChatType.None:
                    return "Party Finder";
                case ChatType.TellIncoming:
                    return "Tell (Incoming)";
                case ChatType.TellOutgoing:
                    return "Tell (Outgoing)";
                case ChatType.CrossParty:
                    return "Party (Cross-world)";
            }

            var lf = data.GetExcelSheet<LogFilter>().FirstOrDefault(lf => lf.LogKind == type.LogKind());
            return lf?.Name?.ToString() ?? type.ToString();
        }

        public static ChatType FromCode(ushort code) {
            return (ChatType) (code & Clear7);
        }

        public static ChatType FromDalamud(XivChatType type) {
            return FromCode((ushort) type);
        }

        public static bool IsBattle(this ChatType type) {
            switch (type) {
                case ChatType.Damage:
                case ChatType.Miss:
                case ChatType.Action:
                case ChatType.Item:
                case ChatType.Healing:
                case ChatType.GainBuff:
                case ChatType.LoseBuff:
                case ChatType.GainDebuff:
                case ChatType.LoseDebuff:
                case ChatType.BattleSystem:
                    return true;
                default:
                    return false;
            }
        }
    }
}
