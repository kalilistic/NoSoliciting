using System;
using System.IO;
using Microsoft.ML;
using NoSoliciting.Interface;

namespace NoSoliciting.CursedWorkaround {
    [Serializable]
    public class CursedWorkaround : MarshalByRefObject, IClassifier, IDisposable {
        private MLContext Context { get; set; } = null!;
        private ITransformer Model { get; set; } = null!;
        private DataViewSchema Schema { get; set; } = null!;
        private PredictionEngine<Data, Prediction> PredictionEngine { get; set; } = null!;

        public override object? InitializeLifetimeService() {
            return null;
        }

        public void Initialise(byte[] data) {
            this.Context = new MLContext();
            this.Context.ComponentCatalog.RegisterAssembly(typeof(Data).Assembly);
            using var stream = new MemoryStream(data);
            var model = this.Context.Model.Load(stream, out var schema);
            this.Model = model;
            this.Schema = schema;
            this.PredictionEngine = this.Context.Model.CreatePredictionEngine<Data, Prediction>(this.Model, this.Schema);
        }

        public string Classify(ushort channel, string message) {
            return this.PredictionEngine.Predict(new Data(channel, message)).Category;
        }

        public void Dispose() {
            this.PredictionEngine.Dispose();
        }
    }
}
