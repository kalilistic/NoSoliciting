using System.IO;
using Microsoft.ML;
using NoSoliciting.Interface;

namespace NoSoliciting.Ml {
    internal class Classifier : IClassifier {
        private MLContext Context { get; set; } = null!;
        private ITransformer Model { get; set; } = null!;
        private DataViewSchema Schema { get; set; } = null!;
        private PredictionEngine<Data, Prediction>? PredictionEngine { get; set; }

        public void Initialise(byte[] data) {
            if (this.PredictionEngine != null) {
                this.PredictionEngine.Dispose();
                this.PredictionEngine = null;
            }

            this.Context = new MLContext();
            this.Context.ComponentCatalog.RegisterAssembly(typeof(Data).Assembly);
            using var stream = new MemoryStream(data);
            var model = this.Context.Model.Load(stream, out var schema);
            this.Model = model;
            this.Schema = schema;
            this.PredictionEngine = this.Context.Model.CreatePredictionEngine<Data, Prediction>(this.Model, this.Schema);
        }

        public string Classify(ushort channel, string message) {
            return this.PredictionEngine?.Predict(new Data(channel, message))?.Category ?? "UNKNOWN";
        }

        public void Dispose() {
            this.PredictionEngine?.Dispose();
        }
    }
}
