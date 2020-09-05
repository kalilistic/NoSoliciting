using Dalamud.Game.Chat;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests {
    public class DefUtils {
        public static object[][] DataFromStrings(string[] strings) => strings.Select(s => new object[] { s }).ToArray();
        public static object[][] DataFromMessages(TestMessage[] messages) => messages.Select(m => new object[] { m }).ToArray();
    }

    public class DefinitionsFixture {
        internal readonly Definitions defs;

        public DefinitionsFixture() {
            this.defs = Definitions.Load(File.ReadAllText("../../../NoSoliciting/definitions.yaml"));

            var allDefs = defs.Chat
                .Concat(defs.PartyFinder)
                .Concat(defs.Global);
            foreach (KeyValuePair<string, Definition> entry in allDefs) {
                entry.Value.Initialise(entry.Key);
            }
        }
    }

    public class TestMessage {
        internal ChatType channel;
        internal string content;

        public TestMessage(string content) : this(ChatType.None, content) { }

        public TestMessage(ChatType channel, string content) {
            this.content = content;
            this.channel = channel;
        }

        public override string ToString() {
            string name = channel == ChatType.None ? "PF" : channel.ToString();
            return $"[{name}] {this.content}";
        }
    }

    public enum CheckType {
        Positive,
        Negative,
    }

    internal static class DefinitionExt {
        internal static void Check(this Definition def, string message, CheckType type) {
            TestMessage testMsg = new TestMessage(message);

            def.Check(testMsg, type);
        }

        internal static void Check(this Definition def, TestMessage message, CheckType type) {
            switch (type) {
                case CheckType.Positive:
                    Assert.True(def.Matches((XivChatType)message.channel, message.content), message.content);
                    break;
                case CheckType.Negative:
                    Assert.False(def.Matches((XivChatType)message.channel, message.content), message.content);
                    break;
            }
        }
    }
}
