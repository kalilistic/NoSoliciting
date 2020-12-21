using Microsoft.ML.Data;

namespace NoSoliciting.Classifier {
    public class MessageData {
        public string? Category { get; }

        public uint Channel { get; }

        public string Message { get; }

        public MessageData(uint channel, string message) {
            this.Channel = channel;
            this.Message = message;
        }
    }

    public class MessagePrediction {
        [ColumnName("PredictedLabel")]
        public string Category { get; set; } = null!;

        [ColumnName("Score")]
        public float[] Probabilities { get; set; } = null!;
    }
}
