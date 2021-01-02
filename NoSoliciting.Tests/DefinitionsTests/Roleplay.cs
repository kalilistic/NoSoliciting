using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests.Global {
    public class Roleplay : DefinitionTest {
        public Roleplay(DefinitionsFixture fixture) {
            this.Def = fixture.Defs.Global["roleplay"];
        }

        public static object[][] DataPositives => DefUtils.DataFromStrings(new[] {
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
            "[18+]Need to get your RP/ERP fix in? Wishing to become or buy a courtesan? C'mere to Touch Fluffy Tail @ discord.gg/fCS8Zng",
            "MR casino venue looking for greeters and courtesans. Join pt if interested.",
            "Have a venue? Come plug yourself while checking us out!  We're The - !! - https://discord.gg/S7BUVKh",
            "Looking for a good time? Come to the Sapphire for frat/sorority night! 5-10EST Gilga Mist W2 P9",
            "Lucky Sevens - 18+ RP community to find partners, advertise your venue, post screenies and enjoy FF14! discord.gg/LuckySevens",
            "{RP} New venue coming to Siren! Join for more information ahead of our grand opening! https://www.discord.gg/nqJtQSD",
            "Open RP night with the Lucky Sevens at The Gold Court, Hyperion’s Steps of Thal X: 11, Y: 11 - discord.gg/LuckySevens",
            "LUST the original Maid Harem- welcome guests into our discord! https://discord.gg/wczaT6k ERP discord 18+",
            "Lucky Sevens - Primal's largest and most active RP discord - Welcomes you! discord.gg/LuckySevens",
            "SPAGHETTI WESTERN NIGHT AT SPAGET 2112! Free cowboy hats! Whiskey provided by the Whiskey Tears! Gilga Mist W21 P12",
            "(RP) Adonis Blue invites you to frolick and play within our land of enchantment. All are welcome to  make merry.Lamia Gob W14 L4.",
            "[18+RP] Teraflare is having a VIP sleepover at 1am EST!! Talk to management, and get your VIP access to join our shenanigans!!",
            "[RP] Karaoke Night at NRAID HQ! Sign up for a chance to perform on our stage! Spectators welcome! https://discord.gg/ZhgqEqf",
            " Coeurlseye Bazaar Night! 9 PM EST - Vendors, food, unique trinkets and more! Learn more at tinyurl.com/CBazaar",
            "[RP] [Siren] The Black Flower Lounge is looking for new staff! waiter, bartender and escort positions open! join for info!",
            "The Starlight Room is hosting a gothic masquerade tonight from 8PM EST til 1AM EST. The Goblet War 10 Plot 19 Sargatanas!",
            "[RP/LGBT+] The Ponspectors present: \"BAKE SALE!\" |Excal|Mist|Ward13|Apartment6| Come see our catboys!",
            "[RP][2 LIVES GONE CAFE] \"Take a break from housing. Inside a house\"  LB W23 P17",
            "Need a break from Bozja? Come relax at Cottontail Cafe and enjoy our food and drink  Exodus Mist 12, 57",
            "[RP] The Reading Nook Come in For Tea,Treats, and the Company of your Fellow Xaela/Raens from the Azim Steppe!",
            "CLUB KARMA serves what you deserve every Wednesday! Join our Discord for more fun and info: https://discord.gg/xmNc7rn",
            "Crescents Keep All Saints Date Auction and Costume Party! Over 1mil in prizes! Mist Ward 14 Plot 34 7-10 EST",
            "Black Lotus HalloweenParty Oct.26th7pmCST!CostumeContest,DJ,  Auction and More!  https://blacklotushalloweenparty.carrd.co/",
            "The Queen's Parlor all-inclusive resort is hiring! Front of house, restaurant, spa and casino all have openings!",
            "\"A Grave Affair\", party on Cactuar, Goblet, W6P6! Costume contest w/ prizes, raffle, and more! https://discord.gg/6X24tx6v",
            "Come pull up to the Bread Bank and buy some bread\nIn the Adamantoise Goblet, Ward 20, plot 40",
            "Come to The House Of Seoul and enjoy Jazz with eastern influence. Relax, eat, or drink. Vibe is a must. Faerie Shiro 24 24",
        });

        public static object[][] DataNegatives => DefUtils.DataFromStrings(new[] {
            "«ToC» recruiting active members. Join the party, send me a /Tell or stop by the FC house (Goblet P13, W19) for more information.",
            "This new group I joined has some newer ultimate raiders in it, one of them hasn't cleared E8S. But I was surprised. We got through p1 of TEA in about 7-8 hours and now we're working on limit cut",
            "Bard LF Static E9S-E12S reclears. Available Sun-Thu 8pm PST onwards, Fri-Sat 1pm PST onwards. Discord: Mountainwhale#0001",
        });

        [Theory]
        [MemberData(nameof(DataPositives))]
        public void Positives(string msg) => this.Check(msg, CheckType.Positive);

        [Theory]
        [MemberData(nameof(DataNegatives))]
        public void Negatives(string msg) => this.Check(msg, CheckType.Negative);
    }
}
