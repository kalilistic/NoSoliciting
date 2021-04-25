using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Dalamud.Game.Text;
using Dalamud.Plugin;
using NoSoliciting.Interface;
using NoSoliciting.Properties;
using YamlDotNet.Core;
using YamlDotNet.Core.Events;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace NoSoliciting {
    public class Definitions {
        public static string? LastError { get; private set; }
        public static DateTime? LastUpdate { get; set; }

        private const string Url = "https://git.sr.ht/~jkcclemens/NoSoliciting/blob/master/NoSoliciting/definitions.yaml";

        public uint Version { get; private set; }
        public Uri ReportUrl { get; private set; }

        public Dictionary<string, Definition> Chat { get; private set; }
        public Dictionary<string, Definition> PartyFinder { get; private set; }

        public static async Task<Definitions> UpdateAndCache(Plugin plugin) {
            #if DEBUG
            return LoadDefaults();
            #endif

            Definitions? defs = null;

            var download = await Download().ConfigureAwait(true);
            if (download != null) {
                defs = download.Item1;

                try {
                    UpdateCache(plugin, download.Item2);
                } catch (IOException e) {
                    PluginLog.Log("Could not update cache.");
                    PluginLog.Log(e.ToString());
                }
            }

            return defs ?? await CacheOrDefault(plugin).ConfigureAwait(true);
        }

        public static Definitions Load(string text) {
            var de = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .WithTypeConverter(new MatcherConverter())
                .IgnoreUnmatchedProperties()
                .Build();
            return de.Deserialize<Definitions>(text);
        }

        private static async Task<Definitions> CacheOrDefault(Plugin plugin) {
            if (plugin == null) {
                throw new ArgumentNullException(nameof(plugin), "Plugin cannot be null");
            }

            var pluginFolder = plugin.Interface.ConfigDirectory.ToString();

            var cachedPath = Path.Combine(pluginFolder, "definitions.yaml");
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
            return LoadDefaults();
        }

        private static Definitions LoadDefaults() {
            return Load(Resources.DefaultDefinitions);
        }

        private static async Task<Tuple<Definitions, string>?> Download() {
            try {
                using var client = new WebClient();
                var text = await client.DownloadStringTaskAsync(Url).ConfigureAwait(true);
                LastError = null;
                return Tuple.Create(Load(text), text);
            } catch (Exception e) when (e is WebException or YamlException) {
                PluginLog.Log("Could not download newest definitions.");
                PluginLog.Log(e.ToString());
                LastError = e.Message;
                return null;
            }
        }

        private static async void UpdateCache(Plugin plugin, string defs) {
            var pluginFolder = plugin.Interface.ConfigDirectory.ToString();
            Directory.CreateDirectory(pluginFolder);
            var cachePath = Path.Combine(pluginFolder, "definitions.yaml");

            var b = Encoding.UTF8.GetBytes(defs);

            using var file = File.OpenWrite(cachePath);
            await file.WriteAsync(b, 0, b.Length).ConfigureAwait(true);
        }

        internal void Initialise(Plugin plugin) {
            var defs = this.Chat.Select(e => new KeyValuePair<string, Definition>($"chat.{e.Key}", e.Value))
                .Concat(this.PartyFinder.Select(e => new KeyValuePair<string, Definition>($"party_finder.{e.Key}", e.Value)));

            foreach (var entry in defs) {
                entry.Value.Initialise(entry.Key);
                if (!plugin.Config.FilterStatus.TryGetValue(entry.Key, out _)) {
                    plugin.Config.FilterStatus[entry.Key] = entry.Value.Default;
                }
            }

            plugin.Config.Save();
        }
    }

    public class Definition {
        private bool _initialised;

        [YamlIgnore]
        public string Id { get; private set; }

        public List<List<Matcher>> RequiredMatchers { get; private set; } = new();
        public List<List<Matcher>> LikelyMatchers { get; private set; } = new();
        public int LikelihoodThreshold { get; private set; }
        public bool IgnoreCase { get; private set; }
        public bool Normalise { get; private set; } = true;
        public List<XivChatType> Channels { get; private set; } = new();
        public OptionNames Option { get; private set; }
        public bool Default { get; private set; }

        public void Initialise(string id) {
            if (this._initialised) {
                return;
            }

            this._initialised = true;

            this.Id = id ?? throw new ArgumentNullException(nameof(id), "string cannot be null");

            if (!this.IgnoreCase) {
                return;
            }

            var allMatchers = this.LikelyMatchers
                .Concat(this.RequiredMatchers)
                .SelectMany(matchers => matchers);

            foreach (var matcher in allMatchers) {
                matcher.MakeIgnoreCase();
            }
        }

        public bool Matches(XivChatType type, string text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text), "string cannot be null");
            }

            if (this.Channels.Count != 0 && !this.Channels.Contains(type)) {
                return false;
            }

            if (this.Normalise) {
                text = NoSolUtil.Normalise(text);
            }

            if (this.IgnoreCase) {
                text = text.ToLowerInvariant();
            }

            // ensure all required matchers match
            var allRequired = this.RequiredMatchers.All(matchers => matchers.Any(matcher => matcher.Matches(text)));
            if (!allRequired) {
                return false;
            }

            // calculate likelihood
            var likelihood = this.LikelyMatchers.Count(matchers => matchers.Any(matcher => matcher.Matches(text)));

            // matches only if likelihood is greater than or equal the threshold
            return likelihood >= this.LikelihoodThreshold;
        }
    }

    public class Matcher {
        private string? substring;
        private Regex? regex;

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
                this.regex = new Regex(this.regex.ToString(), this.regex.Options | RegexOptions.IgnoreCase);
            }
        }

        public bool Matches(string text) {
            if (text == null) {
                throw new ArgumentNullException(nameof(text), "string cannot be null");
            }

            if (this.substring != null) {
                return text.Contains(this.substring);
            }

            if (this.regex != null) {
                return this.regex.IsMatch(text);
            }

            throw new ApplicationException("Matcher created without substring or regex");
        }
    }

    public class OptionNames {
        public string Basic { get; private set; }
        public string Advanced { get; private set; }
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

                var regex = new Regex(parser.Consume<Scalar>().Value, RegexOptions.Compiled);
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
