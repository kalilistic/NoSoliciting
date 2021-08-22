using System;

namespace NoSoliciting.Interface {
    public interface IClassifier : IDisposable {
        void Initialise(byte[] data);

        string Classify(ushort channel, string message);
    }
}
