﻿using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests.Global {
    public class Roleplay : DefinitionTest {
        public Roleplay(DefinitionsFixture fixture) {
            this.Def = fixture.defs.Global["roleplay"];
        }

        public static object[][] DataPositives => DefUtils.DataFromStrings(new string[] {
            "If you're looking for something to do, come find monthly contests, 4 weekly RP events and more! discord.gg/LuckySevens",
            "Thorned Dragon Cosplay Event Sunday 9/6! Website for more info: tdevent.carrd.co Discord: discord.gg/thorneddragonclub",
            "[Rp] The Viridian Orchid is a non-profit companion's den seeking those interested in joining our community! <viriorchid.carrd.co>",
            "[RP] The Thonrned Dragon Cosplay Contest entry closes tonight at midnight est. Entry is free. Full details at [discord.gg/48R4RpP]",
            "● The Pearl ● a Victorian Brothel opening Tuesday! Applications closing tonight, get yours in now! [ thepearlxiv.carrd.co ]",
            "[RP] Looking for adventure? Bounties, work, odd jobs? Join Bounty Call! More info in our discord: https://discord.gg/SnjZWRf",
            "[RP/+18RP] - The HROTHEL - Bathhouse & Bar [LGBTQ+Friendly]\n[Fam. W.5 - P.50 Gridania][discord.io/hrothel] Hrothgar Operated",
            "( RP ) Wonderlust is a new club on Midgar! We are hiring! We also want our customers to join! https://discord.gg/Mn3QNn",
            "Twine Trolley Hostel is now open in Mist, 3rd Ward (Exodus) apt#88. Please be respectful of other guests and enjoy your stay.",
            "DLITE Is OPEN! Come grab a courtesan and relax in our lounge and let our expert staff see to your every whim, SIren, gob W19,43",
            "If you're looking for something to do, come find monthly contests, 4 weekly RP events and more! discord.gg/LuckySevens",
        });

        [Theory]
        [MemberData(nameof(DataPositives))]
        public void Positives(string msg) => this.Check(msg, CheckType.Positive);
    }
}