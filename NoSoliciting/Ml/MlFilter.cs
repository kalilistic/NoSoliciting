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

        private Process Process { get; }
        private IIpcClient<IClassifier> Classifier { get; }

        private MlFilter(uint version, Process process, IIpcClient<IClassifier> classifier) {
            this.Process = process;
            this.Classifier = classifier;
            this.Version = version;
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

            using var exe = Resource.AsStream("NoSoliciting.NoSoliciting.MessageClassifier.exe");
            var pluginFolder = Util.PluginFolder(plugin);
            Directory.CreateDirectory(pluginFolder);
            var exePath = Path.Combine(pluginFolder, "NoSoliciting.MessageClassifier.exe");
            using (var exeFile = File.Create(exePath)) {
                await exe.CopyToAsync(exeFile);
            }

            var startInfo = new ProcessStartInfo(exePath) {
                CreateNoWindow = true,
                UseShellExecute = false,
            };
            var process = Process.Start(startInfo);

            var serviceProvider = new ServiceCollection()
                .AddNamedPipeIpcClient<IClassifier>("client", (_, options) => {
                    options.PipeName = "NoSoliciting.MessageClassifier";
                    options.Serializer = new BetterIpcSerialiser();
                })
                .BuildServiceProvider();

            var clientFactory = serviceProvider.GetRequiredService<IIpcClientFactory<IClassifier>>();
            var client = clientFactory.CreateClient("client");

            await client.InvokeAsync(classifier => classifier.Initialise(data));

            return new MlFilter(manifest.Item1.Version, process!, client);
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
            this.Process.Kill();
            this.Process.Dispose();
        }
    }
}
