namespace NoSoliciting.Interface {
    public interface IClassifier {
        void Initialise(byte[] data);

        string Classify(ushort channel, string message);
    }
}
