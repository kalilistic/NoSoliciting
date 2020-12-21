using System;
using System.IO;
using System.Threading.Channels;
using Microsoft.ML;

namespace NoSoliciting.Classifier {
    public class Classifier : IDisposable {
        private string ConfigPath { get; }

        private MLContext Context { get; }
        private ITransformer Model { get; }
        private DataViewSchema Schema { get; }

        private PredictionEngine<MessageData, MessagePrediction> PredictionEngine { get; }

        public Classifier(string configPath) {
            this.ConfigPath = configPath;

            this.Context = new MLContext();
            this.Model = this.Context.Model.Load(Path.Combine(this.ConfigPath, "model.zip"), out var schema);
            this.Schema = schema;
            this.PredictionEngine = this.Context.Model.CreatePredictionEngine<MessageData, MessagePrediction>(this.Model, this.Schema);
        }

        public string Classify(ushort channel, string message) {
            var data = new MessageData(channel, message);
            var pred = this.PredictionEngine.Predict(data);
            return pred.Category;
        }

        public void Dispose() {
            this.PredictionEngine.Dispose();
        }
    }
}
