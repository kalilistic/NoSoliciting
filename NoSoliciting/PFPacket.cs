using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace NoSoliciting {
    public static class PacketInfo {
        public static readonly int packetSize = Marshal.SizeOf<PFPacket>();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PFPacket {
        private readonly int unk0;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private readonly byte[] padding1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public PFListing[] listings;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PFListing {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 28)]
        private readonly byte[] header1;

        internal readonly ushort duty;
        internal readonly ushort dutyType;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        private readonly byte[] header2;

        internal readonly ushort world;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private readonly byte[] header3;

        internal readonly byte objective;
        internal readonly byte beginnersWelcome;
        internal readonly byte conditions;
        internal readonly byte dutyFinderSettings;
        internal readonly byte lootRules;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        internal readonly byte[] header4; // all zero in every pf I've examined

        internal readonly uint lastPatchHotfixTimestamp; // last time the servers were restarted?
        internal readonly ushort secondsRemaining;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
        private readonly byte[] header5; // 00 00 01 00 00 00 in every pf I've examined

        internal readonly ushort minimumItemLevel;
        internal readonly ushort homeWorld;
        internal readonly ushort currentWorld;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private readonly byte[] header6; // 02 XX 01 00 in every pf I've examined

        internal readonly byte searchArea;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        private readonly byte[] header7; // 00 01 00 00 00 for every pf except alliance raids where it's 01 03 00 00 00 (second byte # parties?)

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        internal readonly uint[] slots;
        private readonly uint job; // job started as?

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private readonly byte[] header8; // all zero in every pf I've examined

        // Note that ByValTStr will not work here because the strings are UTF-8 and there's only a CharSet for UTF-16 in C#.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        private readonly byte[] name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 192)]
        private readonly byte[] description;

        private static string HandleString(byte[] bytes) {
            byte[] nonNull = bytes.TakeWhile(b => b != 0).ToArray();
            return Encoding.UTF8.GetString(nonNull);
        }

        internal string Name() {
            return HandleString(this.name);
        }

        internal string Description() {
            return HandleString(this.description);
        }

        internal bool IsNull() {
            // a valid party finder must have at least one slot set
            return this.slots.All(slot => slot == 0);
        }
    }
}
