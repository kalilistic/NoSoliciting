using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NoSoliciting {
    public class Message {
        public Guid Id { get; private set; }
        public uint DefinitionsVersion { get; private set; }
        public DateTime Timestamp { get; private set; }
        public ChatType ChatType { get; private set; }
        public SeString Sender { get; private set; }
        public SeString Content { get; private set; }
        public string FilterReason { get; private set; }

        public Message(Definitions defs, ChatType type, SeString sender, SeString content, string reason) {
            if (defs == null) {
                throw new ArgumentNullException(nameof(defs), "Definitions cannot be null");
            }

            this.Id = Guid.NewGuid();
            this.DefinitionsVersion = defs.Version;
            this.Timestamp = DateTime.Now;
            this.ChatType = type;
            this.Sender = sender;
            this.Content = content;
            this.FilterReason = reason;
        }

        public Message(Definitions defs, ChatType type, string sender, string content, string reason) : this(
            defs,
            type,
            new SeString(new Payload[] { new TextPayload(sender) }),
            new SeString(new Payload[] { new TextPayload(content) }),
            reason
        ) { }

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
            public string Reason { get; set; }
        }

        public string ToJson() {
            JsonMessage msg = new JsonMessage {
                Id = this.Id,
                DefinitionsVersion = this.DefinitionsVersion,
                Timestamp = this.Timestamp,
                Type = (ushort)this.ChatType,
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
        Ls1 = 16,
        Ls2 = 17,
        Ls3 = 18,
        Ls4 = 19,
        Ls5 = 20,
        Ls6 = 21,
        Ls7 = 22,
        Ls8 = 23,
        FreeCompany = 24,
        NoviceNetwork = 27,
        CustomEmote = 28,
        StandardEmote = 29,
        Yell = 30,
        CrossParty = 32,
        PvPTeam = 36,
        CrossLinkShell1 = 37,
        Echo = 56,
        SystemMessage = 57,
        SystemError = 58,
        GatheringSystemMessage = 59,
        ErrorMessage = 60,
        NpcChat2 = 61,
        ObtainGil = 62,
        NpcChat = 68,
        FCBuff = 69,
        RetainerSale = 71,
        PartyFinderSummary = 72,
        NoviceNetworkSystem = 75,
        CrossLinkShell2 = 101,
        CrossLinkShell3 = 102,
        CrossLinkShell4 = 103,
        CrossLinkShell5 = 104,
        CrossLinkShell6 = 105,
        CrossLinkShell7 = 106,
        CrossLinkShell8 = 107,
        MailSent = 569,
        MailError = 572,
        FCMotd = 581,
        BattleAbility = 2091,
        SystemMessage2 = 2105,
        SelfRevive = 2106,
        SelfGainBuff = 2222,
        SelfLoseBuff = 2224,
        SelfOutgoingDamage = 2729,
        SelfMiss = 2730,
        DealDamage = 2857,
        ActorDefeated = 2874,
        ObtainItem = 2110,
        ObtainExperience = 2112,
        PlayerUsesAbility = 8235,
        UseItem = 8236,
        Revive = 8250,
        LevelUpAchievement = 8256,
        CraftItem = 8258,
        FCJoin = 8261,
        AttackMiss = 8746,
        RecoverHp = 8749,
        GainBuff = 8750,
        LoseBuff = 8752,
        FCLoginLogout = 8774,
        PlayerHitsEnemy = 9001,
        PlayerDefeatsEnemy = 9018,
        BattleEnemyAbility = 10283,
        BattleIncomingDamage = 10409,
        BattleEnemyMiss = 10410,
        EnemyRestoreHp = 10925,
        BattleEnemyAbility2 = 12331,
        PlayerTakesDamage = 12841,
        EnemyHitsFriendlyNpc = 13225,
        FriendlyNpcCast = 14379,
        FriendlyNpcHitsEnemy = 15145,
        FriendlyNpcMissesEnemy = 15146,
        FriendlyNpcGainBuff = 15278,
        FriendlyNpcLoseBuff = 15280,
        PetAbility = 22571,
        PetCausesHpRecovery = 23085,
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1028:Enum Storage should be Int32")]
    public enum ChatCategory : byte {
        Progress = 0,
        Loot = 1,
        Crafting = 2,
        Gathering = 3,
        Damage = 41,
        Miss = 42,
        Ability = 43,
        Item = 44,
        Healing = 45,
        GainBuff = 46,
        GainDebuff = 47,
        LoseBuff = 48,
        LoseDebuff = 49,
        Defeated = 58,
        System = 57,
        GatheringSystem = 59,
        Error = 60,
        ObtainItem = 62,
    }

    public static class ChatTypeExt {
        private const ushort THRESHOLD = 1024;
        private const ushort CLEAR_6 = ~(~0 << 6);

        public static ChatCategory? Category(this ChatType type) {
            if ((ushort)type < THRESHOLD) {
                return null;
            }

            return (ChatCategory)((ushort)type & CLEAR_6);
        }

        public static bool IsBattle(this ChatType type) {
            var category = type.Category();
            if (category == null) {
                return false;
            }

            return category.Value.IsBattle();
        }
    }

    public static class ChatCategoryExt {
        public static bool IsBattle(this ChatCategory category) {
            switch (category) {
                case ChatCategory.Damage:
                case ChatCategory.Miss:
                case ChatCategory.Ability:
                case ChatCategory.Item:
                case ChatCategory.Healing:
                case ChatCategory.GainBuff:
                case ChatCategory.LoseBuff:
                case ChatCategory.GainDebuff:
                case ChatCategory.LoseDebuff:
                case ChatCategory.Defeated:
                    return true;
                default:
                    return false;
            }
        }
    }
}
