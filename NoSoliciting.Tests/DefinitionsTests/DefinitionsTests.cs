using Dalamud.Game.Chat;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace NoSoliciting.Tests.DefinitionsTests {
    public static class DefUtils {
        public static object[][] DataFromStrings(IEnumerable<string> strings) => strings.Select(s => new object[] { s }).ToArray();
        public static object[][] DataFromMessages(IEnumerable<TestMessage> messages) => messages.Select(m => new object[] { m }).ToArray();
    }

    public class DefinitionsFixture {
        internal Definitions Defs { get; }

        public DefinitionsFixture() {
            this.Defs = Definitions.Load(File.ReadAllText("../../../../NoSoliciting/definitions.yaml"));

            var allDefs = this.Defs.Chat
                .Concat(this.Defs.PartyFinder)
                .Concat(this.Defs.Global);
            foreach (var entry in allDefs) {
                entry.Value.Initialise(entry.Key);
            }
        }
    }

    public class TestMessage {
        internal ChatType Channel { get; }
        internal string Content { get; }

        public TestMessage(string content) : this(ChatType.None, content) { }

        public TestMessage(ChatType channel, string content) {
            this.Content = content;
            this.Channel = channel;
        }

        public override string ToString() {
            var name = this.Channel == ChatType.None ? "PF" : this.Channel.ToString();
            return $"[{name}] {this.Content}";
        }
    }

    public enum CheckType {
        Positive,
        Negative,
    }

    internal static class DefinitionExt {
        internal static void Check(this Definition def, string message, CheckType type) {
            var testMsg = new TestMessage(message);

            def.Check(testMsg, type);
        }

        internal static void Check(this Definition def, TestMessage message, CheckType type) {
            switch (type) {
                case CheckType.Positive:
                    Assert.True(def.Matches((XivChatType)message.Channel, message.Content), message.Content);
                    break;
                case CheckType.Negative:
                    Assert.False(def.Matches((XivChatType)message.Channel, message.Content), message.Content);
                    break;
            }
        }
    }
}
