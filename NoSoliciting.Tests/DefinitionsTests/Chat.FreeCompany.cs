using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests.Chat {
    public class FreeCompany : DefinitionTest {
        public FreeCompany(DefinitionsFixture fixture) {
            this.Def = fixture.defs.Chat["free_company"];
        }

        public static object[][] DataPositives => DefUtils.DataFromMessages(new TestMessage[] {
            new TestMessage(ChatType.Shout, "<LUL> is recruiting! Join our community blah blah discord lul"),
        });

        public static object[][] DataNegatives => DefUtils.DataFromMessages(new TestMessage[] {
            new TestMessage(ChatType.Say, "<LUL> is recruiting! Join our community blah blah discord lul"),
        });

        [Theory]
        [MemberData(nameof(DataPositives))]
        public void Positives(TestMessage message) => this.Check(message, CheckType.Positive);

        [Theory]
        [MemberData(nameof(DataNegatives))]
        public void Negatives(TestMessage message) => this.Check(message, CheckType.Negative);
    }
}
