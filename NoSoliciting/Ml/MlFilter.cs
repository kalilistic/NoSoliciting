using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;
using NoSoliciting.Interface;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NoSoliciting.Ml {
    public class MlFilter : IDisposable {
        public static string? LastError { get; private set; }

        private const string ManifestName = "manifest.yaml";
        private const string ModelName = "model.zip";
        private const string Url = "http://localhost:8000/manifest.yaml";

        public uint Version { get; }
        private IClassifier Classifier { get; }

        private MlFilter(uint version, IClassifier classifier) {
            this.Version = version;
            this.Classifier = classifier;
        }

        // private MLContext Context { get; }
        // private ITransformer Model { get; }
        // private DataViewSchema Schema { get; }
        // private PredictionEngine<MessageData, MessagePrediction> PredictionEngine { get; }
        //
        // private MlFilter(uint version, MLContext context, ITransformer model, DataViewSchema schema) {
        //     this.Version = version;
        //     this.Context = context;
        //     this.Model = model;
        //     this.Schema = schema;
        //     this.PredictionEngine = this.Context.Model.CreatePredictionEngine<MessageData, MessagePrediction>(this.Model, this.Schema);
        // }

        public MessageCategory ClassifyMessage(ushort channel, string message) {
            // var data = new MessageData(channel, message);
            // var pred = this.PredictionEngine.Predict(data);
            var rawCategory = this.Classifier.Classify(channel, message);
            var category = MessageCategoryExt.FromString(rawCategory);

            if (category != null) {
                return (MessageCategory) category;
            }

            PluginLog.LogWarning($"Unknown message category: {rawCategory}");
            return MessageCategory.Normal;
        }

        public static async Task<MlFilter?> Load(Plugin plugin) {
            var manifest = await DownloadManifest();
            if (manifest == null) {
                return null;
            }

            byte[]? data = null;

            var localManifest = LoadCachedManifest(plugin);
            if (localManifest != null && localManifest.Version == manifest.Item1.Version) {
                try {
                    data = File.ReadAllBytes(CachedFilePath(plugin, ModelName));
                } catch (IOException) {
                    // ignored
                }
            }

            data ??= await DownloadModel(manifest.Item1.ModelUrl);

            if (data == null) {
                return null;
            }

            UpdateCachedFile(plugin, ModelName, data);
            UpdateCachedFile(plugin, ManifestName, Encoding.UTF8.GetBytes(manifest.Item2));

            // var context = new MLContext();
            // using var stream = new MemoryStream(data);
            // var model = context.Model.Load(stream, out var schema);

            // return new MlFilter(manifest.Item1.Version, context, model, schema);

            plugin.Classifier.Initialise(data);

            return new MlFilter(
                manifest.Item1.Version,
                plugin.Classifier
            );
        }

        private static async Task<byte[]?> DownloadModel(Uri url) {
            try {
                using var client = new WebClient();
                var data = await client.DownloadDataTaskAsync(url);
                return data;
            } catch (WebException e) {
                PluginLog.LogError("Could not download newest model.");
                PluginLog.LogError(e.ToString());
                LastError = e.Message;
                return null;
            }
        }

        private static string CachedFilePath(IDalamudPlugin plugin, string name) {
            var pluginFolder = Util.PluginFolder(plugin);
            Directory.CreateDirectory(pluginFolder);
            return Path.Combine(pluginFolder, name);
        }

        private static async void UpdateCachedFile(IDalamudPlugin plugin, string name, byte[] data) {
            var cachePath = CachedFilePath(plugin, name);

            var file = File.OpenWrite(cachePath);
            await file.WriteAsync(data, 0, data.Length);
            await file.FlushAsync();
            file.Dispose();
        }

        private static async Task<Tuple<Manifest, string>?> DownloadManifest() {
            try {
                using var client = new WebClient();
                var data = await client.DownloadStringTaskAsync(Url);
                LastError = null;
                return Tuple.Create(LoadYaml<Manifest>(data), data);
            } catch (Exception e) when (e is WebException || e is YamlException) {
                PluginLog.LogError("Could not download newest model manifest.");
                PluginLog.LogError(e.ToString());
                LastError = e.Message;
                return null;
            }
        }

        private static Manifest? LoadCachedManifest(IDalamudPlugin plugin) {
            var manifestPath = CachedFilePath(plugin, ManifestName);
            if (!File.Exists(manifestPath)) {
                return null;
            }

            string data;
            try {
                data = File.ReadAllText(manifestPath);
            } catch (IOException) {
                return null;
            }

            try {
                return LoadYaml<Manifest>(data);
            } catch (YamlException) {
                return null;
            }
        }

        private static T LoadYaml<T>(string data) {
            var de = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
            return de.Deserialize<T>(data);
        }

        public void Dispose() {
            // this.PredictionEngine.Dispose();
        }
    }
}
