using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NoSoliciting {
    public class Definitions {
        public static string LastError { get; private set; } = null;
        public static DateTime? LastUpdate { get; set; } = null;

        private const string URL = "https://git.sr.ht/~jkcclemens/NoSoliciting/blob/master/NoSoliciting/definitions.yaml";

        public uint Version { get; private set; }
        public ChatDefinitions Chat { get; private set; }
        public PartyFinderDefinitions PartyFinder { get; private set; }
        public GlobalDefinitions Global { get; private set; }

        public static async Task<Definitions> UpdateAndCache(Plugin plugin) {
            Definitions defs = null;

            var download = await Download().ConfigureAwait(true);
            if (download != null) {
                defs = download.Item1;

                try {
                    UpdateCache(plugin, download.Item2);
                } catch (IOException e) {
                    PluginLog.Log($"Could not update cache.");
                    PluginLog.Log(e.ToString());
                }
            }

            return defs ?? await CacheOrDefault(plugin).ConfigureAwait(true);
        }

        private static Definitions Load(string text) {
            var de = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithTypeConverter(new MatcherConverter())
                .Build();
            return de.Deserialize<Definitions>(text);
        }

        private static string PluginFolder(Plugin plugin) {
            return Path.Combine(new string[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher",
                "pluginConfigs",
                plugin.Name,
            });
        }

        private static async Task<Definitions> CacheOrDefault(Plugin plugin) {
            if (plugin == null) {
                throw new ArgumentNullException(nameof(plugin), "Plugin cannot be null");
            }

            string pluginFolder = PluginFolder(plugin);

            string cachedPath = Path.Combine(pluginFolder, "definitions.yaml");
            if (!File.Exists(cachedPath)) {
                goto LoadDefaults;
            }

            string text;
            using (var file = File.OpenText(cachedPath)) {
                text = await file.ReadToEndAsync().ConfigureAwait(true);
            }

            try {
                return Load(text);
            } catch (YamlException e) {
                PluginLog.Log($"Could not load cached definitions: {e}. Loading defaults.");
            }

        LoadDefaults:
            return await LoadDefaults().ConfigureAwait(true);
        }

        private static async Task<Definitions> LoadDefaults() {
            string defaultPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "default_definitions.yaml"
            );

            string text;
            using (StreamReader file = File.OpenText(defaultPath)) {
                text = await file.ReadToEndAsync().ConfigureAwait(true);
            }

            return Load(text);
        }

        private static async Task<Tuple<Definitions, string>> Download() {
            try {
                using (WebClient client = new WebClient()) {
                    string text = await client.DownloadStringTaskAsync(URL).ConfigureAwait(true);
                    LastError = null;
                    return Tuple.Create(Load(text), text);
                }
            } catch (WebException e) {
                PluginLog.Log($"Could not download newest definitions.");
                PluginLog.Log(e.ToString());
                LastError = e.Message;
                return null;
            }
        }

        private static async void UpdateCache(Plugin plugin, string defs) {
            string pluginFolder = PluginFolder(plugin);
            Directory.CreateDirectory(pluginFolder);
            string cachePath = Path.Combine(pluginFolder, "definitions.yaml");

            byte[] b = Encoding.UTF8.GetBytes(defs);

            using (var file = File.OpenWrite(cachePath)) {
                await file.WriteAsync(b, 0, b.Length).ConfigureAwait(true);
            }
        }

        internal void Initialise() {
            Definition[] all = {
                this.Chat.RMT,
                this.Chat.FreeCompany,
                this.PartyFinder.RMT,
                this.Global.Roleplay,
            };

            foreach (Definition def in all) {
                def.Initialise();
            }
        }
    }

    public class ChatDefinitions {
        [YamlMember(Alias = "rmt")]
        public Definition RMT { get; private set; }
        public Definition FreeCompany { get; private set; }
    }

    public class PartyFinderDefinitions {
        [YamlMember(Alias = "rmt")]
        public Definition RMT { get; private set; }
    }

    public class GlobalDefinitions {
        public Definition Roleplay { get; private set; }
    }

    public class Definition {
        private bool initialised = false;

        public List<List<Matcher>> RequiredMatchers { get; private set; } = new List<List<Matcher>>();
        public List<List<Matcher>> LikelyMatchers { get; private set; } = new List<List<Matcher>>();
        public int LikelihoodThreshold { get; private set; } = 0;
        public bool IgnoreCase { get; private set; } = false;

        internal void Initialise() {
            if (this.initialised) {
                return;
            }

            this.initialised = true;

            if (!this.IgnoreCase) {
                return;
            }

            IEnumerable<Matcher> allMatchers = this.LikelyMatchers
                .Concat(this.RequiredMatchers)
                .SelectMany(matchers => matchers);

            foreach (Matcher matcher in allMatchers) {
                matcher.MakeIgnoreCase();
            }
        }

        public bool Matches(string text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text), "string cannot be null");
            }

            if (this.IgnoreCase) {
                text = text.ToLowerInvariant();
            }

            // ensure all required matchers match
            bool allRequired = this.RequiredMatchers.All(matchers => matchers.Any(matcher => matcher.Matches(text)));
            if (!allRequired) {
                return false;
            }

            // calculate likelihood
            int likelihood = 0;

            foreach (var matchers in this.LikelyMatchers) {
                if (matchers.Any(matcher => matcher.Matches(text))) {
                    likelihood += 1;
                }
            }

            // matches only if likelihood is greater than or equal the threshold
            return likelihood >= this.LikelihoodThreshold;
        }
    }

    public class Matcher {
        private string substring;
        private Regex regex;

        public Matcher(string substring) {
            this.substring = substring ?? throw new ArgumentNullException(nameof(substring), "string cannot be null");
        }

        public Matcher(Regex regex) {
            this.regex = regex ?? throw new ArgumentNullException(nameof(regex), "Regex cannot be null");
        }

        internal void MakeIgnoreCase() {
            if (this.substring != null) {
                this.substring = this.substring.ToLowerInvariant();
            }

            if (this.regex != null) {
                this.regex = new Regex(this.regex.ToString(), regex.Options | RegexOptions.IgnoreCase);
            }
        }

        public bool Matches(string text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text), "string cannot be null");
            }

            if (this.substring != null) {
                return text.Contains(substring);
            }

            if (this.regex != null) {
                return this.regex.IsMatch(text);
            }

            throw new ApplicationException("Matcher created without substring or regex");
        }
    }

    internal sealed class MatcherConverter : IYamlTypeConverter {
        public bool Accepts(Type type) {
            return type == typeof(Matcher);
        }

        public object ReadYaml(IParser parser, Type type) {
            Matcher matcher;

            if (parser.TryConsume(out Scalar scalar)) {
                matcher = new Matcher(scalar.Value);
            } else if (parser.TryConsume(out MappingStart _)) {
                if (parser.Consume<Scalar>().Value != "regex") {
                    throw new ArgumentException("matcher was an object but did not specify regex key");
                }

                Regex regex = new Regex(parser.Consume<Scalar>().Value, RegexOptions.Compiled);
                matcher = new Matcher(regex);

                parser.Consume<MappingEnd>();
            } else {
                throw new ArgumentException("invalid matcher");
            }

            return matcher;
        }

        public void WriteYaml(IEmitter emitter, object value, Type type) {
            throw new NotImplementedException();
        }
    }
}
