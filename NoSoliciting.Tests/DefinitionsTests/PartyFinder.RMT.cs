using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests.PartyFinder {
    public class RMT : DefinitionTest {
        public RMT(DefinitionsFixture fixture) {
            this.Def = fixture.Defs.PartyFinder["rmt"];
        }

        public static object[][] DataPositives => DefUtils.DataFromStrings(new[] {
            "「Best Prices」《 Shiva Unreal ★ Warrior of Light ★ Ultimates ★ Eden's Verse i500/i505",
            "「 MINMAXØ 」 SALES ≪ ❶ Savage 一 ❷ Ultimates 一 ❸ Mounts Etc. ≫ World #1 teams, instant delivery. Discord → azrael#6447",
            "「」™️ Found it Cheaper? We will beat it! $elling EdenVerse, BLU, Ultimates, Primals, Discord: Valentine#5943",
            "「BiS」Selling 》Sac EX, Unreal Shiva 》Savages, Ultimates 》Mounts & More | Price Match Guarantee | Discord→Present#0148",
            "｢ Selling ｣ ♥ Raids Trials Ultimates BLU ♥ Fast Delivery ♥ Price Match ♥ Discord→ Shion#5162",
            "♥ SELLING ♥ Shiva Unreal / WoL XM - Eden Savage (☆ i500/i505 ☆) - TEA/UwU/UCoB - Old raids and + |  Discord add me: gin#5147",
            "「MinmaxØ」 Offering any  →  →  → & more, Instant delivery. Discord→ Minmax#0001",
            "[Viet Rice Farmers] is selling All Content Add on Discord Heyitsjowey#2703",
            "【Selling ー All the content. You want something? We got this!ーHQ teams and speed at your service!】Discord : Victoriam#4716",
            "「」 Guaranteed results! World #1 raiders! ≪ ❶ Savage 一 ❷ Ultimates 一 ❸ Mounts ≫ Discord → ashlar#6021",
            "Primal/Omega/E4S mounts Gil only at  https://rollraider.carrd.co/ or https://discord.gg/FfS5QnW",
            "「   TEA•UWU•UCOB =   $ 」→ Jealous#5404",
            "☀5.4 Pre-orders☀Savage☀Trials☀Ultimates☀BLU ｢ＤＩＳＣＯＲＤ｣⇒ Meliora#2500",
            "●  ＭＩＮ Ｍ ＡＸ Ø | ❶ Loot & Mounts (New Savage, Ultimates etc.) ❷ Coaching & Logs | Instant delivery. Discord→ Enma#7777",
            "【SALES】EDENS PROMISE※ULTIMATES※BLU 【PRICE MATCH】【DISCORD】⇔ akise#8096",
            "  ＭＩＮ Ｍ ＡＸ Ø →  +  +  +  → Instant Delivery & Most Trusted. Discord → Enma#7777 ",
            " WorthyLegendsØ 【2021 Discount for everyone】  ❶ 『EDEN 9S-12S！ALL LOOT』  一  Delivery Now. Discord → Dawn#4022",
            "[LALAKUZA] Eden's Promise Savage i530/535 loot, BLU Morbol, UWU,UCOB,TEA Ultimates, Trials. Discord: Lalakuza#1157",
        });

        public static object[][] DataNegatives => DefUtils.DataFromStrings(new[] {
            "Doing Art commission of your charactet with good price! more info add me on discord: d0uglaz#7409 ♥",
            "Selling HQ 490 DoH/DoL sets, just in time for the Ishgard restoration project. Cheaper than MB, Whipser or join for info.",
            "Looking to sell medium odder otter walls(2mil) join or tell.",
            @"Selling 1x Eldthurs Horn for 8mil. Skip MB taxes \o/ Join if interested",
            "Selling Phanta mats, let me know what you want and I can deliver it to you. 2k for 300k. Join the party don't /tell please",
            "#1 NA WHOLESALER! NEO SET-660K! i490 SET-1.5M(w/DISCOUNT OPTIONS)! BUYING PHANTAS ANY AMOUNT 3K! JOIN PARTY!!!",
            "1/2 chest Lootmaster for sam weap. If weap drops then coffer is rolled on. KB uptime. Ilya. If you just want a page or to help.",
            "Selling ufiti mount for 1.1 mil\nits cheaper then the MB ",
        });

        [Theory]
        [MemberData(nameof(DataPositives))]
        public void Positives(string msg) => this.Check(msg, CheckType.Positive);

        [Theory]
        [MemberData(nameof(DataNegatives))]
        public void Negatives(string msg) => this.Check(msg, CheckType.Negative);
    }
}
