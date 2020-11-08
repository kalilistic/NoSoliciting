using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests.Chat {
    public class RMT : DefinitionTest {
        public RMT(DefinitionsFixture fixture) {
            this.Def = fixture.defs.Chat["rmt"];
        }

        public static object[][] DataPositives => DefUtils.DataFromMessages(new TestMessage[] {
            new TestMessage(ChatType.Shout, "FF14Mog.com selling cheap Mog Station Redeem Code,Dirndl's Attire  $8.99, Chocobo Carriage  $14.39 ,Use 5Off Code:FF5"),
            new TestMessage(ChatType.Say, "----[4KGOLD.COM]----[Best Buy Gil Store]----[Cheapest Price]-----[4KGOLD.COM]---[Ultrafast Deliveryin 10 Mins]--[6OFF Code;LOVE]---359qe"),
            new TestMessage(ChatType.Shout, "【 PVP◇NK.℃ O M 、◇ = BA 】，5分納品！ジル＆480-500HQセット＆希望の園エデン (野蛮)全部強奪!安い&安全保障【コード：714、5％OFF】!!!-ssrum"),
            new TestMessage(ChatType.Shout, "【 PV■NK.℃ O M 、■ = PBA 】，5分納品！ジル＆480-500HQセット＆希望の園エデン (野蛮)全部強奪!安い&安全保障【コード：714、5％OFF】!!!-cfjyf"),
            new TestMessage(ChatType.Shout, "www.ff14mog.com贩売非常に安い  レベルブースト1280円 チョコボキャリッジ 1612円  ディアンドル 1008円 割引コード:JPMOG"),
            new TestMessage(ChatType.Shout, "【 PV■NK.℃ O M 、■ = PBA 】，5分納品！ジル＆480-500HQセット＆希望の園エデン (野蛮)全部強奪!安い&安全保障【コード：714、5％OFF】!!!-unpcp"),
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
