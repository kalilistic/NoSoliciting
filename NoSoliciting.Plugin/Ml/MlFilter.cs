using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using NoSoliciting.Interface;
using NoSoliciting.Resources;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NoSoliciting.Ml {
    public class MlFilter : IDisposable {
        public static string? LastError { get; private set; }

        private const string ManifestName = "manifest.yaml";
        private const string ModelName = "model.zip";
        private const string Url = "https://no-soliciting.nyc3.digitaloceanspaces.com/manifest.yaml";

        public uint Version { get; }
        public Uri ReportUrl { get; }

        private IClassifier Classifier { get; }

        private MlFilter(uint version, Uri reportUrl, IClassifier classifier) {
            this.Classifier = classifier;
            this.Version = version;
            this.ReportUrl = reportUrl;
        }

        public MessageCategory ClassifyMessage(ushort channel, string message) {
            var prediction = this.Classifier.Classify(channel, message);
            var category = MessageCategoryExt.FromString(prediction);

            if (category != null) {
                return (MessageCategory) category;
            }

            Plugin.Log.Warning($"Unknown message category: {prediction}");
            return MessageCategory.Normal;
        }

        public static async Task<MlFilter?> Load(Plugin plugin, bool showWindow) {
            plugin.MlStatus = MlFilterStatus.DownloadingManifest;

            var manifest = await DownloadManifest();
            if (manifest == null) {
                Plugin.Log.Warning("Could not download manifest. Will attempt to fall back on cached version.");
            } else {
                Plugin.Log.Info($"Downloaded manifest version {manifest.Value.manifest.Version}");
            }

            byte[]? data = null;

            var localManifest = LoadCachedManifest(plugin);
            if (localManifest != null && (manifest?.Item1 == null || localManifest.Version == manifest.Value.manifest.Version)) {
                try {
                    Plugin.Log.Info("Using cached model since it is up to date.");
                    data = await File.ReadAllBytesAsync(CachedFilePath(plugin, ModelName));
                    manifest ??= (localManifest, string.Empty);
                } catch (IOException) {
                    Plugin.Log.Info("Cached model is missing or corrupted.");
                }
            }

            if (!string.IsNullOrEmpty(manifest?.source)) {
                plugin.MlStatus = MlFilterStatus.DownloadingModel;
                data ??= await DownloadModel(manifest!.Value.manifest!.ModelUrl);
            }

            if (data == null) {
                Plugin.Log.Warning("Could not download model.");
                plugin.MlStatus = MlFilterStatus.Uninitialised;
                return null;
            }

            if (!string.IsNullOrEmpty(manifest!.Value.source)) {
                Plugin.Log.Info("Updating cached files.");
                UpdateCachedFile(plugin, ModelName, data);
                UpdateCachedFile(plugin, ManifestName, Encoding.UTF8.GetBytes(manifest.Value.source));
            }

            plugin.MlStatus = MlFilterStatus.Initialising;

            var classifier = new Classifier();
            classifier.Initialise(data);

            return new MlFilter(
                manifest.Value.manifest!.Version,
                manifest.Value.manifest!.ReportUrl,
                classifier
            );
        }

        private static async Task<byte[]?> DownloadModel(Uri url) {
            try {
                Plugin.Log.Info("Downloading model from {0}", url);
                using var client = new WebClient();
                var data = await client.DownloadDataTaskAsync(url);
                return data;
            } catch (WebException e) {
                Plugin.Log.Error("Could not download newest model.");
                Plugin.Log.Error(e.ToString());
                LastError = e.Message;
                return null;
            }
        }

        private static string CachedFilePath(Plugin plugin, string name) {
            var pluginFolder = plugin.Interface.ConfigDirectory.ToString();
            Directory.CreateDirectory(pluginFolder);
            return Path.Combine(pluginFolder, name);
        }

        private static async void UpdateCachedFile(Plugin plugin, string name, byte[] data) {
            var cachePath = CachedFilePath(plugin, name);

            var file = File.Create(cachePath);
            await file.WriteAsync(data, 0, data.Length);
            await file.FlushAsync();
            await file.DisposeAsync();
        }

        private static async Task<(Manifest manifest, string source)?> DownloadManifest() {
            try {
                using var client = new WebClient();
                var data = await client.DownloadStringTaskAsync(Url);
                LastError = null;
                return (LoadYaml<Manifest>(data), data);
            } catch (Exception e) when (e is WebException or YamlException) {
                Plugin.Log.Error("Could not download newest model manifest.");
                Plugin.Log.Error(e.ToString());
                LastError = e.Message;
                return null;
            }
        }

        private static Manifest? LoadCachedManifest(Plugin plugin) {
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
            this.Classifier.Dispose();
        }
    }

    public enum MlFilterStatus {
        Uninitialised,
        Preparing,
        DownloadingManifest,
        DownloadingModel,
        Initialising,
        Initialised,
    }

    public static class MlFilterStatusExt {
        public static string Description(this MlFilterStatus status) {
            return status switch {
                MlFilterStatus.Uninitialised => Language.ModelStatusUninitialised,
                MlFilterStatus.Preparing => Language.ModelStatusPreparing,
                MlFilterStatus.DownloadingManifest => Language.ModelStatusDownloadingManifest,
                MlFilterStatus.DownloadingModel => Language.ModelStatusDownloadingModel,
                MlFilterStatus.Initialising => Language.ModelStatusInitialising,
                MlFilterStatus.Initialised => Language.ModelStatusInitialised,
                _ => status.ToString(),
            };
        }
    }
}
