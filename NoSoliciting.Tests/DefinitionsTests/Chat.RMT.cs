using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests.Chat {
    public class RMT : DefinitionTest {
        public RMT(DefinitionsFixture fixture) {
            this.Def = fixture.defs.Chat["rmt"];
        }

        public static object[][] DataPositives => DefUtils.DataFromMessages(new[] {
            new TestMessage(ChatType.Shout, "FF14Mog.com selling cheap Mog Station Redeem Code,Dirndl's Attire  $8.99, Chocobo Carriage  $14.39 ,Use 5Off Code:FF5"),
            new TestMessage(ChatType.Say, "----[4KGOLD.COM]----[Best Buy Gil Store]----[Cheapest Price]-----[4KGOLD.COM]---[Ultrafast Deliveryin 10 Mins]--[6OFF Code;LOVE]---359qe"),
            new TestMessage(ChatType.Shout, "【 PVP◇NK.℃ O M 、◇ = BA 】，5分納品！ジル＆480-500HQセット＆希望の園エデン (野蛮)全部強奪!安い&安全保障【コード：714、5％OFF】!!!-ssrum"),
            new TestMessage(ChatType.Shout, "【 PV■NK.℃ O M 、■ = PBA 】，5分納品！ジル＆480-500HQセット＆希望の園エデン (野蛮)全部強奪!安い&安全保障【コード：714、5％OFF】!!!-cfjyf"),
            new TestMessage(ChatType.Shout, "www.ff14mog.com贩売非常に安い  レベルブースト1280円 チョコボキャリッジ 1612円  ディアンドル 1008円 割引コード:JPMOG"),
            new TestMessage(ChatType.Shout, "【 PV■NK.℃ O M 、■ = PBA 】，5分納品！ジル＆480-500HQセット＆希望の園エデン (野蛮)全部強奪!安い&安全保障【コード：714、5％OFF】!!!-unpcp"),
            new TestMessage(ChatType.Say, "5GOLD.COM--Buy FFXIV Gil Cheapest,100% Handwork Guaranteed,24/7 online service[5% OFF Code;GOLD].12456"),
            new TestMessage(ChatType.Shout, "【 PVP●K.℃ O M 、● = BAN 】，5分納品！ジル＆480-500HQセット＆希望の園エデン (野蛮)全部強奪!安い&安全保障【コード：714、5％OFF】!!!-kgthx"),
            new TestMessage(ChatType.Shout, "【 PVP●K.℃ O M 、● = BAN 】，5分納品！ジル＆480-500HQセット＆希望の園エデン (野蛮)全部強奪!安い&安全保障【コード：714、5％OFF】!!!-pdonb"),
            new TestMessage(ChatType.Say, "Buy Cheap gils on www,G/a/m/e/r/E/a/s/y.c0m, 8% code,FFXIV2020, 15 mins deliveryvnm15"),
            new TestMessage(ChatType.TellIncoming, "You're Invited To Our Clan's Final Giveaway Of 850M Gil Starting In 45Mins! Visit Our Discord For The Location Of The Giveaway: https://discord.gg/VtqY9tFyUb"),
            new TestMessage(ChatType.TellIncoming, "Quitting FFXIV, You're Invited To The Final Giveaway Of 850M Gil! Please Read The Rules And Location Of The Giveaway On The FFXIV Forum Post: https://www.squarenix.com-iy.ru/ffxiv/threads/6842918"),
            new TestMessage(ChatType.Say, "5GOLD.COM--Cheapest ffxiv gil online-5 Min delivery-24/7 online service[5% OFF Code;GOLD].hfzcb"),
        });

        //public static object[][] DataNegatives => DefUtils.DataFromMessages(new TestMessage[] {

        //});

        [Theory]
        [MemberData(nameof(DataPositives))]
        public void Positives(TestMessage message) => this.Check(message, CheckType.Positive);

        //[Theory]
        //[MemberData(nameof(DataNegatives))]
        //public void Negatives(TestMessage message) => this.Check(message, CheckType.Negative);
    }
}
