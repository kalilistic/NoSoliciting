using System;
using System.IO;
using System.IO.Compression;
using Microsoft.ML;
using NoSoliciting.Interface;

namespace NoSoliciting.Ml {
    internal class Classifier : IClassifier {
        private MLContext Context { get; set; } = null!;
        private ITransformer BinaryModel { get; set; } = null!;
        private ITransformer MultiClassModel { get; set; } = null!;
        private PredictionEngine<DataBinary, PredictionBinary>? BinaryPredictionEngine { get; set; }
        private PredictionEngine<Data, Prediction>? MultiClassPredictionEngine { get; set; }

        public void Initialise(byte[] data) {
            DisposeEngines();

            this.Context = new MLContext();
            this.Context.ComponentCatalog.RegisterAssembly(typeof(DataBinary).Assembly);

            using var stream = new MemoryStream(data);
            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            var binaryEntry = archive.GetEntry("model_binary.zip");
            var multiclassEntry = archive.GetEntry("model_multiclass.zip");

            if (binaryEntry == null || multiclassEntry == null) {
                throw new InvalidDataException("Required model files not found in the archive.");
            }

            using var binaryStream = binaryEntry.Open();
            using var multiclassStream = multiclassEntry.Open();

            this.BinaryModel = this.Context.Model.Load(binaryStream, out _);
            this.MultiClassModel = this.Context.Model.Load(multiclassStream, out _);

            this.BinaryPredictionEngine = this.Context.Model.CreatePredictionEngine<DataBinary, PredictionBinary>(this.BinaryModel);
            this.MultiClassPredictionEngine = this.Context.Model.CreatePredictionEngine<Data, Prediction>(this.MultiClassModel);
        }

        public string Classify(ushort channel, string message) {
            if (this.BinaryPredictionEngine == null || this.MultiClassPredictionEngine == null) {
                throw new InvalidOperationException("Classifier is not initialized.");
            }

            var binaryPrediction = this.BinaryPredictionEngine.Predict(new DataBinary {
                Channel = channel,
                Message = message
            });

            if (binaryPrediction.PredictedIsNormal) {
                return "NORMAL";
            }

            var multiclassPrediction = this.MultiClassPredictionEngine.Predict(new Data(channel, message));
            return multiclassPrediction.Category;
        }

        public void Dispose() {
            DisposeEngines();
        }

        private void DisposeEngines() {
            this.BinaryPredictionEngine?.Dispose();
            this.MultiClassPredictionEngine?.Dispose();
            this.BinaryPredictionEngine = null;
            this.MultiClassPredictionEngine = null;
        }
    }
}
