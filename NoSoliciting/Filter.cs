using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin;
using System;
using System.Runtime.InteropServices;

namespace NoSoliciting {
    public partial class Filter : IDisposable {
        private readonly Plugin plugin;
        private bool clearOnNext = false;

        private delegate void HandlePFPacketDelegate(IntPtr param_1, IntPtr param_2);
        private readonly Hook<HandlePFPacketDelegate> handlePacketHook;

        private delegate long HandlePFSummaryDelegate(long param_1, long param_2);
        private readonly Hook<HandlePFSummaryDelegate> handleSummaryHook;

        private bool disposedValue;

        public Filter(Plugin plugin) {
            this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin cannot be null");

            IntPtr listingPtr = this.plugin.Interface.TargetModuleScanner.ScanText("40 53 41 57 48 83 EC 28 48 8B D9");
            IntPtr summaryPtr = this.plugin.Interface.TargetModuleScanner.ScanText("48 89 5C 24 ?? 57 48 83 EC 20 48 8B D9 48 8B FA 48 8B 49 ?? 48 8B 01 FF 90 ?? ?? ?? ?? 48 8B C8 BA 79 00 00 00 E8 ?? ?? ?? ?? 48 85 C0 74 ?? 44 0F B6 83 ?? ?? ?? ??");
            if (listingPtr == IntPtr.Zero || summaryPtr == IntPtr.Zero) {
                PluginLog.Log("Party Finder filtering disabled because hook could not be created.");
                return;
            }

            this.handlePacketHook = new Hook<HandlePFPacketDelegate>(listingPtr, new HandlePFPacketDelegate(this.HandlePFPacket));
            this.handlePacketHook.Enable();

            this.handleSummaryHook = new Hook<HandlePFSummaryDelegate>(summaryPtr, new HandlePFSummaryDelegate(this.HandleSummary));
            this.handleSummaryHook.Enable();
        }

        private void HandlePFPacket(IntPtr param_1, IntPtr param_2) {
            if (this.plugin.Definitions == null) {
                this.handlePacketHook.Original(param_1, param_2);
                return;
            }

            if (this.clearOnNext) {
                this.plugin.ClearPartyFinderHistory();
                this.clearOnNext = false;
            }

            IntPtr dataPtr = param_2 + 0x10;

            // parse the packet into a struct
            PFPacket packet = Marshal.PtrToStructure<PFPacket>(dataPtr);

            for (int i = 0; i < packet.listings.Length; i++) {
                PFListing listing = packet.listings[i];

                // only look at listings that aren't null
                if (listing.IsNull()) {
                    continue;
                }

                string desc = listing.Description();

                string reason = null;
                bool filter = false;

                filter = filter || (this.plugin.Config.FilterHugeItemLevelPFs
                    && listing.minimumItemLevel > FilterUtil.MaxItemLevelAttainable(this.plugin.Interface.Data)
                    && SetReason(ref reason, "ilvl"));

                foreach (Definition def in this.plugin.Definitions.PartyFinder.Values) {
                    filter = filter || (this.plugin.Config.FilterStatus.TryGetValue(def.Id, out bool enabled)
                        && enabled
                        && def.Matches(XivChatType.None, desc)
                        && SetReason(ref reason, def.Id));
                }

                // check for custom filters if enabled
                filter = filter || (this.plugin.Config.CustomPFFilter
                    && PartyFinder.MatchesCustomFilters(desc, this.plugin.Config)
                    && SetReason(ref reason, "custom"));

                this.plugin.AddPartyFinderHistory(new Message(
                    type: ChatType.None,
                    sender: listing.Name(),
                    content: listing.Description(),
                    reason: reason
                ));

                if (!filter) {
                    continue;
                }

                // replace the listing with an empty one
                packet.listings[i] = new PFListing();

                PluginLog.Log($"Filtered PF listing from {listing.Name()} ({reason}): {listing.Description()}");
            }

            // get some memory for writing to
            byte[] newPacket = new byte[PacketInfo.packetSize];
            GCHandle pinnedArray = GCHandle.Alloc(newPacket, GCHandleType.Pinned);
            IntPtr pointer = pinnedArray.AddrOfPinnedObject();

            // write our struct into the memory (doing this directly crashes the game)
            Marshal.StructureToPtr(packet, pointer, false);

            // copy our new memory over the game's
            Marshal.Copy(newPacket, 0, dataPtr, PacketInfo.packetSize);

            // free memory
            pinnedArray.Free();

            // call original function
            this.handlePacketHook.Original(param_1, param_2);
        }

        private long HandleSummary(long param_1, long param_2) {
            this.clearOnNext = true;

            return this.handleSummaryHook.Original(param_1, param_2);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "fulfilling a delegate")]
        public void OnChat(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message), "SeString cannot be null");
            }

            if (this.plugin.Definitions == null || ((ChatType)type).IsBattle()) {
                return;
            }

            string text = message.TextValue;

            string reason = null;
            bool filter = false;

            foreach (Definition def in this.plugin.Definitions.Chat.Values) {
                filter = filter || (this.plugin.Config.FilterStatus.TryGetValue(def.Id, out bool enabled)
                    && enabled
                    && def.Matches(type, text)
                    && SetReason(ref reason, def.Id));
            }

            // check for custom filters if enabled
            filter = filter || (this.plugin.Config.CustomChatFilter
                && Chat.MatchesCustomFilters(text, this.plugin.Config)
                && SetReason(ref reason, "custom"));

            this.plugin.AddMessageHistory(new Message(
                type: (ChatType)type,
                sender: sender,
                content: message,
                reason: reason
            ));

            if (!filter) {
                return;
            }

            PluginLog.Log($"Filtered chat message ({reason}): {text}");
            isHandled = true;
        }

        private static bool SetReason(ref string reason, string value) {
            reason = value;
            return true;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    this.handlePacketHook?.Dispose();
                    this.handleSummaryHook?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
