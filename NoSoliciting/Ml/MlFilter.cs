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

        public static async Task<MlFilter?> Load(Plugin plugin) {
            plugin.MlStatus = MlFilterStatus.DownloadingManifest;

            var manifest = await DownloadManifest();
            if (manifest == null) {
                PluginLog.LogWarning("Could not download manifest. Will attempt to fall back on cached version.");
            }

            byte[]? data = null;

            var localManifest = LoadCachedManifest(plugin);
            if (localManifest != null && (manifest?.Item1 == null || localManifest.Version == manifest.Item1.Version)) {
                try {
                    data = File.ReadAllBytes(CachedFilePath(plugin, ModelName));
                    manifest ??= Tuple.Create(localManifest, string.Empty);
                } catch (IOException) {
                    // ignored
                }
            }

            if (!string.IsNullOrEmpty(manifest?.Item2)) {
                plugin.MlStatus = MlFilterStatus.DownloadingModel;
                data ??= await DownloadModel(manifest!.Item1!.ModelUrl);
            }

            if (data == null) {
                plugin.MlStatus = MlFilterStatus.Uninitialised;
                return null;
            }

            plugin.MlStatus = MlFilterStatus.Initialising;

            if (!string.IsNullOrEmpty(manifest!.Item2)) {
                UpdateCachedFile(plugin, ModelName, data);
                UpdateCachedFile(plugin, ManifestName, Encoding.UTF8.GetBytes(manifest.Item2));
            }

            var pluginFolder = Util.PluginFolder(plugin);

            var exePath = await ExtractClassifier(pluginFolder);

            var pipeId = Guid.NewGuid();

            var process = StartClassifier(exePath, pipeId);
            var client = await CreateClassifierClient(pipeId, data);

            return new MlFilter(
                manifest.Item1!.Version,
                manifest.Item1!.ReportUrl,
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

        private static Process StartClassifier(string exePath, Guid pipeId) {
            var game = Process.GetCurrentProcess();

            var startInfo = new ProcessStartInfo(exePath) {
                CreateNoWindow = true,
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
