using System;

namespace NoSoliciting.Ml {
    [Serializable]
    public class Manifest {
        public uint Version { get; set; }
        public Uri ModelUrl { get; set; } = null!;
        public string ModelHash { get; set; } = null!;
        public Uri ReportUrl { get; set; } = null!;

        public byte[] Hash() => Convert.FromBase64String(this.ModelHash);
    }
}
