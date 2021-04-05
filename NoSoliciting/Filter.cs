using Dalamud.Hooking;
using Dalamud.Plugin;
using System;
using Dalamud.Game.Internal.Gui;
using Dalamud.Game.Internal.Gui.Structs;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using NoSoliciting.Ml;

namespace NoSoliciting {
    public partial class Filter : IDisposable {
        private const uint MinWords = 4;

        public static readonly ChatType[] FilteredChatTypes = {
            ChatType.Say,
            ChatType.Yell,
            ChatType.Shout,
            ChatType.TellIncoming,
            ChatType.Party,
            ChatType.CrossParty,
            ChatType.Alliance,
            ChatType.FreeCompany,
            ChatType.PvpTeam,
            ChatType.CrossLinkshell1,
            ChatType.CrossLinkshell2,
            ChatType.CrossLinkshell3,
            ChatType.CrossLinkshell4,
            ChatType.CrossLinkshell5,
            ChatType.CrossLinkshell6,
            ChatType.CrossLinkshell7,
            ChatType.CrossLinkshell8,
            ChatType.Linkshell1,
            ChatType.Linkshell2,
            ChatType.Linkshell3,
            ChatType.Linkshell4,
            ChatType.Linkshell5,
            ChatType.Linkshell6,
            ChatType.Linkshell7,
            ChatType.Linkshell8,
            ChatType.NoviceNetwork,
        };

        private Plugin Plugin { get; }
        private bool _clearOnNext;

        private delegate IntPtr HandlePfSummaryDelegate(IntPtr param1, IntPtr param2, byte param3);

        private readonly Hook<HandlePfSummaryDelegate>? _handleSummaryHook;

        private bool _disposedValue;

        public Filter(Plugin plugin) {
            this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin cannot be null");

            this.Plugin.Interface.Framework.Gui.Chat.OnCheckMessageHandled += this.OnChat;
            this.Plugin.Interface.Framework.Gui.PartyFinder.ReceiveListing += this.OnListing;

            var summaryPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B FA 48 8B F1 45 84 C0 74 ?? 0F B7 0A");

            this._handleSummaryHook = new Hook<HandlePfSummaryDelegate>(summaryPtr, new HandlePfSummaryDelegate(this.HandleSummary));
            this._handleSummaryHook.Enable();
        }

        private void Dispose(bool disposing) {
            if (this._disposedValue) {
                return;
            }

            if (disposing) {
                this.Plugin.Interface.Framework.Gui.Chat.OnCheckMessageHandled -= this.OnChat;
                this.Plugin.Interface.Framework.Gui.PartyFinder.ReceiveListing -= this.OnListing;
                this._handleSummaryHook?.Dispose();
            }

            this._disposedValue = true;
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void OnChat(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
            isHandled = isHandled || this.FilterMessage(type, sender, message);
        }

        private void OnListing(PartyFinderListing listing, PartyFinderListingEventArgs args) {
            try {
                if (this._clearOnNext) {
                    this.Plugin.ClearPartyFinderHistory();
                    this._clearOnNext = false;
                }

                string? reason = null;
                uint? version = null;
                if (this.Plugin.MlReady) {
                    version = this.Plugin.MlFilter!.Version;
                    reason = this.MlListingFilterReason(listing);
                } else if (this.Plugin.DefsReady) {
                    version = this.Plugin.Definitions!.Version;
                    reason = this.DefsListingFilterReason(listing);
                }

                if (version == null) {
                    return;
                }

                this.Plugin.AddPartyFinderHistory(new Message(
                    version.Value,
                    ChatType.None,
                    listing.Name,
                    listing.Description,
                    this.Plugin.MlReady,
                    reason
                ));

                if (reason == null) {
                    return;
                }

                args.Visible = false;

                if (this.Plugin.Config.LogFilteredPfs) {
                    PluginLog.Log($"Filtered PF listing from {listing.Name.TextValue} ({reason}): {listing.Description.TextValue}");
                }
            } catch (Exception ex) {
                PluginLog.LogError($"Error in PF listing event: {ex}");
            }
        }

        private bool FilterMessage(XivChatType type, SeString sender, SeString message) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message), "SeString cannot be null");
            }

            if (this.Plugin.MlReady) {
                return this.MlFilterMessage(type, sender, message);
            }

            return this.Plugin.DefsReady && this.DefsFilterMessage(type, sender, message);
        }

        private bool MlFilterMessage(XivChatType type, SeString sender, SeString message) {
            if (this.Plugin.MlFilter == null) {
                return false;
            }

            var chatType = ChatTypeExt.FromDalamud(type);

            // NOTE: don't filter on user-controlled chat types here because custom filters are supposed to check all
            //       messages except battle messages
            if (chatType.IsBattle()) {
                return false;
            }

            var text = message.TextValue;

            string? reason = null;

            // step 1. check for custom filters if enabled
            var filter = this.Plugin.Config.CustomChatFilter
                         && Chat.MatchesCustomFilters(text, this.Plugin.Config)
                         && SetReason(out reason, "custom");

            // only look at ml if message >= min words
            if (!filter && text.Trim().Split(' ').Length >= MinWords) {
                // step 2. classify the message using the model
                var category = this.Plugin.MlFilter.ClassifyMessage((ushort) chatType, text);

                // step 2a. only filter if configured to act on this channel
                filter = category != MessageCategory.Normal
                         && this.Plugin.Config.MlEnabledOn(category, chatType)
                         && SetReason(out reason, category.Name());
            }

            this.Plugin.AddMessageHistory(new Message(
                this.Plugin.MlFilter.Version,
                ChatTypeExt.FromDalamud(type),
                sender,
                message,
                true,
                reason
            ));

            if (filter && this.Plugin.Config.LogFilteredChat) {
                PluginLog.Log($"Filtered chat message ({reason}): {text}");
            }

            return filter;
        }

        private bool DefsFilterMessage(XivChatType type, SeString sender, SeString message) {
            if (this.Plugin.Definitions == null || ChatTypeExt.FromDalamud(type).IsBattle()) {
                return false;
            }

            var text = message.TextValue;

            string? reason = null;
            var filter = false;

            foreach (var def in this.Plugin.Definitions.Chat.Values) {
                filter = filter || this.Plugin.Config.FilterStatus.TryGetValue(def.Id, out var enabled)
                    && enabled
                    && def.Matches(type, text)
                    && SetReason(out reason, def.Id);
            }

            // check for custom filters if enabled
            filter = filter || this.Plugin.Config.CustomChatFilter
                && Chat.MatchesCustomFilters(text, this.Plugin.Config)
                && SetReason(out reason, "custom");

            this.Plugin.AddMessageHistory(new Message(
                this.Plugin.Definitions.Version,
                ChatTypeExt.FromDalamud(type),
                sender,
                message,
                false,
                reason
            ));

            if (filter && this.Plugin.Config.LogFilteredChat) {
                PluginLog.Log($"Filtered chat message ({reason}): {text}");
            }

            return filter;
        }

        private string? MlListingFilterReason(PartyFinderListing listing) {
            if (this.Plugin.MlFilter == null) {
                return null;
            }

            // ignore private listings if configured
            if (!this.Plugin.Config.ConsiderPrivatePfs && listing[SearchAreaFlags.Private]) {
                return null;
            }

            var desc = listing.Description.TextValue;

            // step 1. check if pf has an item level that's too high
            if (this.Plugin.Config.FilterHugeItemLevelPFs && listing.MinimumItemLevel > FilterUtil.MaxItemLevelAttainable(this.Plugin.Interface.Data)) {
                return "ilvl";
            }

            // step 2. check custom filters
            if (this.Plugin.Config.CustomPFFilter && PartyFinder.MatchesCustomFilters(desc, this.Plugin.Config)) {
                return "custom";
            }

            // only look at ml for pfs >= min words
            if (desc.Trim().Split(' ').Length < MinWords) {
                return null;
            }

            var category = this.Plugin.MlFilter.ClassifyMessage((ushort) ChatType.None, desc);

            if (category != MessageCategory.Normal && this.Plugin.Config.MlEnabledOn(category, ChatType.None)) {
                return category.Name();
            }

            return null;
        }

        private string? DefsListingFilterReason(PartyFinderListing listing) {
            if (this.Plugin.Definitions == null) {
                return null;
            }

            var desc = listing.Description.TextValue;

            if (this.Plugin.Config.FilterHugeItemLevelPFs && listing.MinimumItemLevel > FilterUtil.MaxItemLevelAttainable(this.Plugin.Interface.Data)) {
                return "ilvl";
            }

            foreach (var def in this.Plugin.Definitions.PartyFinder.Values) {
                if (this.Plugin.Config.FilterStatus.TryGetValue(def.Id, out var enabled) && enabled && def.Matches(XivChatType.None, desc)) {
                    return def.Id;
                }
            }

            // check for custom filters if enabled
            if (this.Plugin.Config.CustomPFFilter && PartyFinder.MatchesCustomFilters(desc, this.Plugin.Config)) {
                return "custom";
            }

            return null;
        }

        private IntPtr HandleSummary(IntPtr param1, IntPtr param2, byte param3) {
            this._clearOnNext = true;

            return this._handleSummaryHook!.Original(param1, param2, param3);
        }

        private static bool SetReason(out string reason, string value) {
            reason = value;
            return true;
        }
    }
}
