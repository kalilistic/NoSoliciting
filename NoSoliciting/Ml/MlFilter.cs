using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Plugin;
using JKang.IpcServiceFramework.Client;
using Microsoft.Extensions.DependencyInjection;
using NoSoliciting.Interface;
using Resourcer;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NoSoliciting.Ml {
    public class MlFilter : IDisposable {
        public static string? LastError { get; private set; }

        private const string ManifestName = "manifest.yaml";
        private const string ModelName = "model.zip";
        #if DEBUG
        private const string Url = "http://localhost:8000/manifest.yaml";
        #else
        private const string Url = "https://annaclemens.io/assets/nosol/ml/manifest.yaml";
        #endif

        public uint Version { get; }
        public Uri ReportUrl { get; }

        private Process Process { get; }
        private IIpcClient<IClassifier> Classifier { get; }

        private MlFilter(uint version, Uri reportUrl, Process process, IIpcClient<IClassifier> classifier) {
            this.Process = process;
            this.Classifier = classifier;
            this.Version = version;
            this.ReportUrl = reportUrl;
        }

        public MessageCategory ClassifyMessage(ushort channel, string message) {
            var prediction = this.Classifier.InvokeAsync(classifier => classifier.Classify(channel, message)).Result;
            var category = MessageCategoryExt.FromString(prediction);

            if (category != null) {
                return (MessageCategory) category;
            }

            PluginLog.LogWarning($"Unknown message category: {prediction}");
            return MessageCategory.Normal;
        }

        public static async Task<MlFilter?> Load(Plugin plugin, bool showWindow) {
            plugin.MlStatus = MlFilterStatus.DownloadingManifest;

            // download and parse the remote manifest
            var manifest = await DownloadManifest();
            if (manifest == null) {
                PluginLog.LogWarning("Could not download manifest. Will attempt to fall back on cached version.");
            }

            // model zip file data
            byte[]? data = null;

            // load the cached manifest
            var localManifest = LoadCachedManifest(plugin);
            // if there is a cached manifest and we either couldn't download/parse the remote OR the cached version is the same as remote version
            if (localManifest != null && (manifest?.Item1 == null || localManifest.Version == manifest.Value.manifest.Version)) {
                try {
                    // try to reach the cached model
                    data = File.ReadAllBytes(CachedFilePath(plugin, ModelName));
                    // set the manifest to our local one and an empty string for the source
                    manifest ??= (localManifest, string.Empty);
                } catch (IOException) {
                    // ignored
                }
            }

            // if there is source for the manifest
            if (!string.IsNullOrEmpty(manifest?.source)) {
                plugin.MlStatus = MlFilterStatus.DownloadingModel;
                // download the model if necessary
                data ??= await DownloadModel(manifest!.Value.manifest!.ModelUrl);
            }

            // give up if we couldn't get any data at this point
            if (data == null) {
                plugin.MlStatus = MlFilterStatus.Uninitialised;
                return null;
            }

            plugin.MlStatus = MlFilterStatus.Initialising;

            // if there is source for the manifest
            if (!string.IsNullOrEmpty(manifest!.Value.source)) {
                // update the cached files
                UpdateCachedFile(plugin, ModelName, data);
                UpdateCachedFile(plugin, ManifestName, Encoding.UTF8.GetBytes(manifest.Value.source));
            }

            // initialise the classifier
            var pluginFolder = plugin.Interface.ConfigDirectory.ToString();

            var exePath = await ExtractClassifier(pluginFolder);

            var pipeId = Guid.NewGuid();

            var process = StartClassifier(exePath, pipeId, showWindow);
            var client = await CreateClassifierClient(pipeId, data);

            return new MlFilter(
                manifest.Value.manifest!.Version,
                manifest.Value.manifest!.ReportUrl,
                process!,
                client
            );
        }

        private static async Task<IIpcClient<IClassifier>> CreateClassifierClient(Guid pipeId, byte[] data) {
            var serviceProvider = new ServiceCollection()
                .AddNamedPipeIpcClient<IClassifier>("client", (_, options) => {
                    options.PipeName = $"NoSoliciting.MessageClassifier-{pipeId}";
                    options.Serializer = new BetterIpcSerialiser();
                })
                .BuildServiceProvider();

            var clientFactory = serviceProvider.GetRequiredService<IIpcClientFactory<IClassifier>>();
            var client = clientFactory.CreateClient("client");

            await client.InvokeAsync(classifier => classifier.Initialise(data));
            return client;
        }

        private static Process StartClassifier(string exePath, Guid pipeId, bool showWindow) {
            var game = Process.GetCurrentProcess();

            var startInfo = new ProcessStartInfo(exePath) {
                CreateNoWindow = !showWindow,
                UseShellExecute = false,
                Arguments = $"\"{game.Id}\" \"{game.ProcessName}\" \"{pipeId}\"",
            };
            return Process.Start(startInfo)!;
        }

        private static async Task<string> ExtractClassifier(string pluginFolder) {
            using var exe = Resource.AsStream("NoSoliciting.NoSoliciting.MessageClassifier.exe");
            Directory.CreateDirectory(pluginFolder);
            var exePath = Path.Combine(pluginFolder, "NoSoliciting.MessageClassifier.exe");

            try {
                using var exeFile = File.Create(exePath);
                await exe.CopyToAsync(exeFile);
            } catch (IOException ex) {
                PluginLog.LogWarning($"Could not update classifier. Continuing as normal.\n{ex}");
            }

            return exePath;
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
            file.Dispose();
        }

        private static async Task<(Manifest manifest, string source)?> DownloadManifest() {
            try {
                using var client = new WebClient();
                var data = await client.DownloadStringTaskAsync(Url);
                LastError = null;
                return (LoadYaml<Manifest>(data), data);
            } catch (Exception e) when (e is WebException or YamlException) {
                PluginLog.LogError("Could not download newest model manifest.");
                PluginLog.LogError(e.ToString());
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
            try {
                this.Process.Kill();
                this.Process.Dispose();
            } catch (Exception) {
                // ignored
            }
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
                MlFilterStatus.Uninitialised => "Uninitialised",
                MlFilterStatus.Preparing => "Preparing to update model",
                MlFilterStatus.DownloadingManifest => "Downloading model manifest",
                MlFilterStatus.DownloadingModel => "Downloading model",
                MlFilterStatus.Initialising => "Initialising model and classifier",
                MlFilterStatus.Initialised => "Initialised",
                _ => status.ToString(),
            };
        }
    }
}
