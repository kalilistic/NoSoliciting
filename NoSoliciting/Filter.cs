﻿using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Internal.Network;
using Dalamud.Plugin;
using System;
using System.Runtime.InteropServices;

namespace NoSoliciting {
    public partial class Filter {
        private const ushort PF_LISTING = 0x252;
        //private static ushort PF_SUMMARY = 0x174;

        private readonly Plugin plugin;

        public Filter(Plugin plugin) {
            this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin cannot be null");
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "fulfilling a delegate")]
        public void OnNetwork(IntPtr dataPtr, ushort opCode, uint sourceActorId, uint targetActorId, NetworkMessageDirection direction) {
            if (this.plugin.Definitions == null) {
                return;
            }

            // only look at packets coming in
            if (direction != NetworkMessageDirection.ZoneDown) {
                return;
            }

            // PF_LISTING is sent repeatedly until PF_SUMMARY, which is a summary (and also the packet sent for the chat notifs)
            if (opCode != PF_LISTING) {
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

                foreach (Definition def in this.plugin.Definitions.PartyFinder.Values) {
                    filter |= this.plugin.Config.FilterStatus.TryGetValue(def.Id, out bool enabled)
                        && enabled
                        && def.Matches(XivChatType.None, desc);
                }

                filter |= this.plugin.Config.FilterHugeItemLevelPFs && listing.minimumItemLevel > FilterUtil.MaxItemLevelAttainable(this.plugin.Interface.Data);

                // check for custom filters if enabled
                filter |= this.plugin.Config.CustomPFFilter && PartyFinder.MatchesCustomFilters(desc, this.plugin.Config);

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
                filter |= this.plugin.Config.FilterStatus.TryGetValue(def.Id, out bool enabled)
                    && enabled
                    && def.Matches(type, text);
            }

            // check for custom filters if enabled
            filter |= this.plugin.Config.CustomChatFilter && Chat.MatchesCustomFilters(text, this.plugin.Config);

            if (!filter) {
                return;
            }

            PluginLog.Log($"Filtered chat message: {text}");
            isHandled = true;
        }
    }
}
