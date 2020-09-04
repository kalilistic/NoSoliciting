using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests.PartyFinder {
    public class RMT : DefinitionTest {
        public RMT(DefinitionsFixture fixture) {
            this.Def = fixture.defs.PartyFinder["rmt"];
        }

        public static object[][] DataNegatives => DefUtils.DataFromStrings(new string[] {
            "Doing Art commission of your charactet with good price! more info add me on discord: d0uglaz#7409 ♥",
            "Selling HQ 490 DoH/DoL sets, just in time for the Ishgard restoration project. Cheaper than MB, Whipser or join for info.",
            "Looking to sell medium odder otter walls(2mil) join or tell.",
            @"Selling 1x Eldthurs Horn for 8mil. Skip MB taxes \o/ Join if interested",
        });

        public static object[][] DataPositives => DefUtils.DataFromStrings(new string[] {
            "「Best Prices」《 Shiva Unreal ★ Warrior of Light ★ Ultimates ★ Eden's Verse i500/i505",
            "「 MINMAXØ 」 SALES ≪ ❶ Savage 一 ❷ Ultimates 一 ❸ Mounts Etc. ≫ World #1 teams, instant delivery. Discord → azrael#6447",
            "「」™️ Found it Cheaper? We will beat it! $elling EdenVerse, BLU, Ultimates, Primals, Discord: Valentine#5943",
            "「BiS」Selling 》Sac EX, Unreal Shiva 》Savages, Ultimates 》Mounts & More | Price Match Guarantee | Discord→Present#0148",
        });

        [Theory]
        [MemberData(nameof(DataNegatives))]
        public void Negatives(string msg) => this.Check(msg, CheckType.Negative);

        [Theory]
        [MemberData(nameof(DataPositives))]
        public void Positives(string msg) => this.Check(msg, CheckType.Positive);
    }
}
