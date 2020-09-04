using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests {
    public abstract class DefinitionTest : IClassFixture<DefinitionsFixture> {
        protected Definition Def { get; set; }

        protected void Check(string message, CheckType type) => this.Def.Check(message, type);
        protected void Check(TestMessage message, CheckType type) => this.Def.Check(message, type);

        // an assortment of normal party finders/messages to make sure no crazy false positives are happening
        public static object[][] DataGlobalNegatives = DefUtils.DataFromMessages(new TestMessage[] {
            // party finders
            new TestMessage("Static LF the listed roles in prep for 5.4 || T/W/Th 7:30-9:30 AM EST || Discord: Mia#0585"),
            new TestMessage("Looking to learn second half of the fight, and then if all goes well farm!!"),
            new TestMessage("T/N H/S Tethers DPS Towers KB prevention dps uptime, Partners for the rest no salt farm party"),
            new TestMessage("another day another camp on the finder! u know the drill by now bahaprog or nael clean up "),
            new TestMessage("♡Need help with anything?♥ Chat, hangout, and can craft! Levekits, gear, & more!☂ Pop in, take a seat, & maybe I can help! :)"),

            // messages
            new TestMessage(ChatType.FreeCompany, "forgot to leave a commendation, I forget this sometimes"),
            new TestMessage(ChatType.StandardEmote, "Anri Valmet laughs at Kyoya Kazuki."),
            new TestMessage(ChatType.Shout, "Need a lvl 80 crafter? Buy a leve kit and get your crafter today! PST for price info :D"),
        });

        [Theory]
        [MemberData(nameof(DataGlobalNegatives))]
        public void GlobalNegatives(TestMessage msg) => this.Check(msg, CheckType.Negative);
    }
}
