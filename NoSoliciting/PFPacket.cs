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
        public byte[] padding1;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public PFListing[] listings;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct PFListing {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
        public byte[] header;
        // Note that ByValTStr will not work here because the strings are UTF-8 and there's only a CharSet for UTF-16 in C#.
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        private readonly byte[] name;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 192)]
        private readonly byte[] description;

        private static string HandleString(byte[] bytes) {
            byte[] nonNull = bytes.TakeWhile(b => b != 0).ToArray();
            return Encoding.UTF8.GetString(nonNull);
        }

        public string Name() {
            return HandleString(this.name);
        }

        public string Description() {
            return HandleString(this.description);
        }
    }
}
