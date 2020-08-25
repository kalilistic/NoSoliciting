using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Hooking;
using Dalamud.Plugin;
using System;
using System.Runtime.InteropServices;

namespace NoSoliciting {
    public partial class Filter : IDisposable {
        private readonly Plugin plugin;

        private delegate void HandlePFPacketDelegate(IntPtr param_1, IntPtr param_2);
        private readonly Hook<HandlePFPacketDelegate> handlePacketHook;
        private bool disposedValue;

        public Filter(Plugin plugin) {
            this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin cannot be null");

            IntPtr delegatePtr = this.plugin.Interface.TargetModuleScanner.ScanText("40 53 41 57 48 83 EC 28 48 8B D9");
            if (delegatePtr == IntPtr.Zero) {
                PluginLog.Log("Party Finder filtering disabled because hook could not be created.");
                return;
            }

            this.handlePacketHook = new Hook<HandlePFPacketDelegate>(delegatePtr, new HandlePFPacketDelegate(this.HandlePFPacket));
            this.handlePacketHook.Enable();
        }

        private void HandlePFPacket(IntPtr param_1, IntPtr param_2) {
            if (this.plugin.Definitions == null) {
                this.handlePacketHook.Original(param_1, param_2);
                return;
            }

            // parse the packet into a struct
            PFPacket packet = Marshal.PtrToStructure<PFPacket>(dataPtr);

            for (int i = 0; i < packet.listings.Length; i++) {
                PFListing listing = packet.listings[i];

                // only look at listings that aren't null
                if (listing.IsNull()) {
                    continue;
                }

                string desc = listing.Description();

                bool filter = false;

                filter = filter || (this.plugin.Config.FilterHugeItemLevelPFs && listing.minimumItemLevel > FilterUtil.MaxItemLevelAttainable(this.plugin.Interface.Data));

                foreach (Definition def in this.plugin.Definitions.PartyFinder.Values) {
                    filter = filter || (this.plugin.Config.FilterStatus.TryGetValue(def.Id, out bool enabled)
                        && enabled
                        && def.Matches(XivChatType.None, desc));
                }


                // check for custom filters if enabled
                filter = filter || (this.plugin.Config.CustomPFFilter && PartyFinder.MatchesCustomFilters(desc, this.plugin.Config));

                if (!filter) {
                    continue;
                }

                // replace the listing with an empty one
                packet.listings[i] = new PFListing();

                PluginLog.Log($"Filtered PF listing from {listing.Name()}: {listing.Description()}");
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "fulfilling a delegate")]
        public void OnChat(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled) {
            if (message == null) {
                throw new ArgumentNullException(nameof(message), "SeString cannot be null");
            }

            if (this.plugin.Definitions == null) {
                return;
            }

            string text = message.TextValue;

            bool filter = false;

            foreach (Definition def in this.plugin.Definitions.Chat.Values) {
                filter = filter || (this.plugin.Config.FilterStatus.TryGetValue(def.Id, out bool enabled)
                    && enabled
                    && def.Matches(type, text));
            }

            // check for custom filters if enabled
            filter = filter || (this.plugin.Config.CustomChatFilter && Chat.MatchesCustomFilters(text, this.plugin.Config));

            if (!filter) {
                return;
            }

            PluginLog.Log($"Filtered chat message: {text}");
            isHandled = true;
        }

        protected virtual void Dispose(bool disposing) {
            if (!disposedValue) {
                if (disposing) {
                    this.handlePacketHook?.Dispose();
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
