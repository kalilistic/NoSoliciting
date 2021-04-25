using System;

namespace NoSoliciting.Ml {
    [Serializable]
    public class Manifest {
        public uint Version { get; set; }
        public Uri ModelUrl { get; set; }
        public string ModelHash { get; set; }
        public Uri ReportUrl { get; set; }

        public byte[] Hash() => Convert.FromBase64String(this.ModelHash);
    }
}
