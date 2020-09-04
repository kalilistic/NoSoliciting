using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests.Chat {
    public class RMT : DefinitionTest {
        public RMT(DefinitionsFixture fixture) {
            this.Def = fixture.defs.Chat["rmt"];
        }

        public static object[][] DataPositives => DefUtils.DataFromMessages(new TestMessage[] {
            new TestMessage(ChatType.Shout, "FF14Mog.com selling cheap Mog Station Redeem Code,Dirndl's Attire  $8.99, Chocobo Carriage  $14.39 ,Use 5Off Code:FF5"),
            new TestMessage(ChatType.Say, "----[4KGOLD.COM]----[Best Buy Gil Store]----[Cheapest Price]-----[4KGOLD.COM]---[Ultrafast Deliveryin 10 Mins]--[6OFF Code;LOVE]---359qe"),
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
