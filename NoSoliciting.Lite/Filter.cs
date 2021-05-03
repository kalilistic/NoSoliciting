using System;
using System.Linq;
using Dalamud.Game.Internal.Gui;
using Dalamud.Game.Internal.Gui.Structs;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Plugin;

namespace NoSoliciting.Lite {
    public class Filter : IDisposable {
        private Plugin Plugin { get; }

        internal Filter(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.Framework.Gui.Chat.OnChatMessage += this.OnChat;
            this.Plugin.Interface.Framework.Gui.PartyFinder.ReceiveListing += this.ReceiveListing;
        }

        public void Dispose() {
            this.Plugin.Interface.Framework.Gui.PartyFinder.ReceiveListing -= this.ReceiveListing;
            this.Plugin.Interface.Framework.Gui.Chat.OnChatMessage -= this.OnChat;
        }

        private void OnChat(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
            if (isHandled) {
                return;
            }

            var text = message.TextValue;

            isHandled = this.Plugin.Config.ValidChatSubstrings.Any(needle => text.ContainsIgnoreCase(needle))
                        || this.Plugin.Config.CompiledChatRegexes.Any(needle => needle.IsMatch(text));

            if (this.Plugin.Config.LogFilteredChat && isHandled) {
                PluginLog.Log($"Filtered chat message: {text}");
            }
        }

        private void ReceiveListing(PartyFinderListing listing, PartyFinderListingEventArgs args) {
            if (!args.Visible) {
                return;
            }

            if (listing[SearchAreaFlags.Private] && !this.Plugin.Config.ConsiderPrivatePfs) {
                return;
            }

            var text = listing.Description.TextValue;

            args.Visible = !(this.Plugin.Config.ValidPfSubstrings.Any(needle => text.ContainsIgnoreCase(needle))
                             || this.Plugin.Config.CompiledPfRegexes.Any(needle => needle.IsMatch(text)));

            if (this.Plugin.Config.LogFilteredPfs && !args.Visible) {
                PluginLog.Log($"Filtered PF: {text}");
            }
        }
    }
}
