using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin;
using System;
using System.Runtime.InteropServices;
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

        private delegate void HandlePfPacketDelegate(IntPtr param1, IntPtr param2);

        private readonly Hook<HandlePfPacketDelegate>? _handlePacketHook;

        private delegate IntPtr HandlePfSummaryDelegate(IntPtr param1, IntPtr param2, byte param3);

        private readonly Hook<HandlePfSummaryDelegate>? _handleSummaryHook;

        private bool _disposedValue;

        public Filter(Plugin plugin) {
            this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin cannot be null");

            this.Plugin.Interface.Framework.Gui.Chat.OnCheckMessageHandled += this.OnChat;

            var listingPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("40 53 41 57 48 83 EC 28 48 8B D9");
            var summaryPtr = this.Plugin.Interface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 48 89 74 24 ?? 57 48 83 EC 20 48 8B FA 48 8B F1 45 84 C0 74 ?? 0F B7 0A");

            this._handlePacketHook = new Hook<HandlePfPacketDelegate>(listingPtr, new HandlePfPacketDelegate(this.TransformPfPacket));
            this._handlePacketHook.Enable();

            this._handleSummaryHook = new Hook<HandlePfSummaryDelegate>(summaryPtr, new HandlePfSummaryDelegate(this.HandleSummary));
            this._handleSummaryHook.Enable();
        }

        public void OnChat(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
            isHandled = isHandled || this.FilterMessage(type, sender, message);
        }

        private void TransformPfPacket(IntPtr param1, IntPtr data) {
            if (data == IntPtr.Zero) {
                goto Return;
            }

            try {
                if (this.Plugin.MlReady) {
                    this.MlTransformPfPacket(data);
                } else if (this.Plugin.DefsReady) {
                    this.DefsTransformPfPacket(data);
                }
            } catch (Exception ex) {
                PluginLog.LogError($"Error in PF hook: {ex}");
            }

            Return:
            this._handlePacketHook!.Original(param1, data);
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

        private void MlTransformPfPacket(IntPtr data) {
            if (this.Plugin.MlFilter == null) {
                return;
            }

            if (this._clearOnNext) {
                this.Plugin.ClearPartyFinderHistory();
                this._clearOnNext = false;
            }

            var dataPtr = data + 0x10;

            // parse the packet into a struct
            var packet = Marshal.PtrToStructure<PfPacket>(dataPtr);

            for (var i = 0; i < packet.listings.Length; i++) {
                var listing = packet.listings[i];

                // only look at listings that aren't null
                if (listing.IsNull()) {
                    continue;
                }

                // ignore private listings if configured
                if (!this.Plugin.Config.ConsiderPrivatePfs && (listing.searchArea & (1 << 1)) != 0) {
                    continue;
                }

                var rawName = listing.Name(this.Plugin.Interface.SeStringManager);
                var rawDesc = listing.Description(this.Plugin.Interface.SeStringManager);

                var name = rawName.TextValue;
                var desc = rawDesc.TextValue;

                string? reason = null;

                // step 1. check if pf has an item level that's too high
                var filter = this.Plugin.Config.FilterHugeItemLevelPFs
                             && listing.minimumItemLevel > FilterUtil.MaxItemLevelAttainable(this.Plugin.Interface.Data)
                             && SetReason(out reason, "ilvl");

                // step 2. check custom filters
                filter = filter || this.Plugin.Config.CustomPFFilter
                    && PartyFinder.MatchesCustomFilters(desc, this.Plugin.Config)
                    && SetReason(out reason, "custom");

                // only look at ml for pfs >= min words
                if (!filter && desc.Trim().Split(' ').Length >= MinWords) {
                    // step 3. check the model's prediction
                    var category = this.Plugin.MlFilter.ClassifyMessage((ushort) ChatType.None, desc);

                    // step 3a. filter the message if configured to do so
                    filter = category != MessageCategory.Normal
                             && this.Plugin.Config.MlEnabledOn(category, ChatType.None)
                             && SetReason(out reason, category.Name());
                }

                this.Plugin.AddPartyFinderHistory(new Message(
                    this.Plugin.MlFilter.Version,
                    ChatType.None,
                    rawName,
                    rawDesc,
                    true,
                    reason
                ));

                if (!filter) {
                    continue;
                }

                // replace the listing with an empty one
                packet.listings[i] = new PfListing();

                if (this.Plugin.Config.LogFilteredPfs) {
                    PluginLog.Log($"Filtered PF listing from {name} ({reason}): {desc}");
                }
            }

            // get some memory for writing to
            var newPacket = new byte[PacketInfo.PacketSize];
            var pinnedArray = GCHandle.Alloc(newPacket, GCHandleType.Pinned);
            var pointer = pinnedArray.AddrOfPinnedObject();

            // write our struct into the memory (doing this directly crashes the game)
            Marshal.StructureToPtr(packet, pointer, false);

            // copy our new memory over the game's
            Marshal.Copy(newPacket, 0, dataPtr, PacketInfo.PacketSize);

            // free memory
            pinnedArray.Free();
        }

        private void DefsTransformPfPacket(IntPtr data) {
            if (this.Plugin.Definitions == null) {
                return;
            }

            if (this._clearOnNext) {
                this.Plugin.ClearPartyFinderHistory();
                this._clearOnNext = false;
            }

            var dataPtr = data + 0x10;

            // parse the packet into a struct
            var packet = Marshal.PtrToStructure<PfPacket>(dataPtr);

            for (var i = 0; i < packet.listings.Length; i++) {
                var listing = packet.listings[i];

                // only look at listings that aren't null
                if (listing.IsNull()) {
                    continue;
                }

                var rawName = listing.Name(this.Plugin.Interface.SeStringManager);
                var rawDesc = listing.Description(this.Plugin.Interface.SeStringManager);

                var name = rawName.TextValue;
                var desc = rawDesc.TextValue;

                string? reason = null;
                var filter = false;

                filter = filter || this.Plugin.Config.FilterHugeItemLevelPFs
                    && listing.minimumItemLevel > FilterUtil.MaxItemLevelAttainable(this.Plugin.Interface.Data)
                    && SetReason(out reason, "ilvl");

                foreach (var def in this.Plugin.Definitions.PartyFinder.Values) {
                    filter = filter || this.Plugin.Config.FilterStatus.TryGetValue(def.Id, out var enabled)
                        && enabled
                        && def.Matches(XivChatType.None, desc)
                        && SetReason(out reason, def.Id);
                }

                // check for custom filters if enabled
                filter = filter || this.Plugin.Config.CustomPFFilter
                    && PartyFinder.MatchesCustomFilters(desc, this.Plugin.Config)
                    && SetReason(out reason, "custom");

                this.Plugin.AddPartyFinderHistory(new Message(
                    this.Plugin.Definitions.Version,
                    ChatType.None,
                    rawName,
                    rawDesc,
                    false,
                    reason
                ));

                if (!filter) {
                    continue;
                }

                // replace the listing with an empty one
                packet.listings[i] = new PfListing();

                if (this.Plugin.Config.LogFilteredPfs) {
                    PluginLog.Log($"Filtered PF listing from {name} ({reason}): {desc}");
                }
            }

            // get some memory for writing to
            var newPacket = new byte[PacketInfo.PacketSize];
            var pinnedArray = GCHandle.Alloc(newPacket, GCHandleType.Pinned);
            var pointer = pinnedArray.AddrOfPinnedObject();

            // write our struct into the memory (doing this directly crashes the game)
            Marshal.StructureToPtr(packet, pointer, false);

            // copy our new memory over the game's
            Marshal.Copy(newPacket, 0, dataPtr, PacketInfo.PacketSize);

            // free memory
            pinnedArray.Free();
        }

        private IntPtr HandleSummary(IntPtr param1, IntPtr param2, byte param3) {
            this._clearOnNext = true;

            return this._handleSummaryHook!.Original(param1, param2, param3);
        }

        private static bool SetReason(out string reason, string value) {
            reason = value;
            return true;
        }

        private void Dispose(bool disposing) {
            if (this._disposedValue) {
                return;
            }

            if (disposing) {
                this.Plugin.Interface.Framework.Gui.Chat.OnCheckMessageHandled -= this.OnChat;
                this._handlePacketHook?.Dispose();
                this._handleSummaryHook?.Dispose();
            }

            this._disposedValue = true;
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
