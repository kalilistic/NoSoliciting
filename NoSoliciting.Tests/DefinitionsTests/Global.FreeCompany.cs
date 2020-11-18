using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests.Global {
    public class FreeCompany : DefinitionTest {
        public FreeCompany(DefinitionsFixture fixture) {
            this.Def = fixture.defs.Global["free_company"];
        }

        public static object[][] DataPositives => DefUtils.DataFromMessages(new TestMessage[] {
            // chat
            new TestMessage(ChatType.Shout, "PhD: Phantasy Degree is a Rank 30 FC with a Large plot, 24/7 FC/EXP Buffs. There are NO Level Restrictions to join, we take new and old players. Ask to join or apply."),
            new TestMessage(ChatType.Shout, "Wind up fc is a small company looking for new/seasoned adventures to join! If you need help with the msq/clear the latest raid well do our best to help! Send a tell/app to join"),
            new TestMessage(ChatType.Shout, "Porxie Menace is recruiting! Veterans & newbies are welcome to join our plan to take over the world... with a snort!  Raiding, mapping, eureka, potd/hoh, pvp and much more!!"),
            new TestMessage(ChatType.Shout, "Looking for an 18+ FC that enjoys all aspects of the game? <Lusty> (Rank 30) is here for you. From RP to Hunts; Whether you're new or a vet, we're a tight-knit group offering assistance to those who'd like it. We are a different kind of Free Company than what you'd expect. We have 24/7 buffs, discord, and of course are a judgement free LGBTQA+ friendly group ;) just click on my name and send a tell to get started!"),
            new TestMessage(ChatType.Shout, "join my free company. we don't got much, but we could have you. which probably also isn't much. join anyway. maps on wednesdays, discord if you feel like it, new player friendly. Ask about a house tour. <<GECKO>> /tell"),
            new TestMessage(ChatType.Shout, "♡♡ PomHub <Pom> is recruiting new and veteran players to join our growing family! | 18 + | LGBTQ Friendly | Housing | Daily Buffs | Events | Discord |"),
            new TestMessage(ChatType.Shout, "Have you been running alone looking for the sweeter things on Faerie? Wanting to find a rowdy, yet caring, bunch of friends? Well, look no further than <CANDY>! we are a social rank 30 FC, friendly and willing to help new or returning players! Have a discord and Large House at Shiro! 18+, RP friendly, LGBTQ friendly as well. Send a tell and join today :hearts:"),
            new TestMessage(ChatType.Shout, "Need an FC? Why not come home to Amaurot? A small group with weekly events, active discord, players primarily on 3 - 11 pm EST /tell me for more info!"),
            new TestMessage(ChatType.Shout, "<Memoria> is a small, social FC looking for new or active players who regularly want to socialize and do content together. If this sounds like something you’re interested in, send me a /tell or apply via an application"),
            new TestMessage(ChatType.Shout, @"Paw Paw Grrr is currently recruiting! We are friendly bunch with constant mood for weird ideas! New? Veteran? Crafter? Doesn't matter! \tell me or Ophelia Shepard for inv :)"),
            new TestMessage(ChatType.Shout, "We have a beautiful mansion in the Mist with all comforts, a fleet farming for us and +20% battle exp buff on Saturdays! Send me a /tell if you want more info or a invite!"),
            new TestMessage(ChatType.Shout, "Nothing is a small and cozy FC looking for members! If emptiness is endless, then everything rests in nothingness. DM me to join on our endless journey!"),
            new TestMessage(ChatType.Shout, "Tired of wandering through the game a lonely weeb? Want Senpai to notice you?! Fat Chocobo FC is now recruiting! Top Ranked FC on Exodus & Worldwide, 6 yrs+ running strong!"),
            new TestMessage(ChatType.Shout, "Ashes Of Aesir <<Aesir>> FC is recruiting! we are a friendly bunch in search of expanding our wings with friendly and active members."),
            new TestMessage(ChatType.TellIncoming, "<<>> Eclipsehey what about joining a  fc full of  people? Low & High end Casual FC (24/7buffs). Accept, Decline or PM me♡."),
            new TestMessage(ChatType.TellIncoming, "<<>>Eclipse would like you to join a - Low & High end Casual FC (24/7buffs). Accept, Decline or PM me♡."),
            new TestMessage(ChatType.TellIncoming, "Hello!;-) I've just send u an invite to join Artemis Moonlight. Join us if u like! We are a friendly and helpful FC, willing to help all level players and for fun teamplay. Kupo!"),

            // party finder
            new TestMessage("FC recruiting new and experienced players. Interested? Join party, send me a /tell or stop by the FC house for more information. "),
            new TestMessage(@"Golden Crow [FC] offering new and old players a save place to hang and have fun with - join the weebs now \o/ /pm for info "),
            new TestMessage("«ToC» recruiting active members. Join the party, send me a /Tell or stop by the FC house (Goblet P13, W19) for more information."),
            new TestMessage("FC Toxic looking for new memeber, Fc house, master crafter and gatherer, buffs available for new players as well as end game"),
            new TestMessage("<Panic> is recruiting! We're a slowly growing fc that would appreciate some new faces. /tell for more info or an inv <3"),
            new TestMessage("Free Company | Fallen Angel | is welcoming new players on Gilgamesh"),
            new TestMessage("[FC recruitment] Small/New FC looking for more members to join us.  New and experienced welcomed. Send tell if interested!"),
            new TestMessage("<WICH> FC is now recruiting!! we are looking for all types of players. PM me for more info :slight_smile:"),
            new TestMessage("Rhotano Mercantile <RM-RP> (18+ RP FC) is recruiting neutral and evil characters for dark RP, enquire within! https://rm-rp.carrd.co/"),
            new TestMessage("Elemancia is a Chill FC looking for people who enjoy EX Trials, Animanga, Zombies and Foodie Stuff, Bible Study Available."),
            new TestMessage("Sol Verinia FC is recruiting! We accept all levels, looking for active members. We look forward to you becoming one of the spire."),
        });

        public static object[][] DataNegatives => DefUtils.DataFromMessages(new TestMessage[] {
            new TestMessage("Static recruit. Not hardcore. Discord needed. tues-thurs 11:30pmEST. Join if you have questions."),
            new TestMessage("LF new LGBT friends to chill with in Eorzea! Join up, let's chat, and hang out. I have discord as well :)"),
            new TestMessage("(Bored)Lovely Au Ra woman [Famfrit] available to be RP girlfriend/wife for long term or short term. Send tell if in need."),
        });

        [Theory]
        [MemberData(nameof(DataPositives))]
        public void Positives(TestMessage message) => this.Check(message, CheckType.Positive);

        [Theory]
        [MemberData(nameof(DataNegatives))]
        public void Negatives(TestMessage message) => this.Check(message, CheckType.Negative);
    }
}
