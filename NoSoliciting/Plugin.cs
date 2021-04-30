using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud;
using NoSoliciting.Interface;
using NoSoliciting.Ml;
using NoSoliciting.Resources;
using XivCommon;

namespace NoSoliciting {
    public class Plugin : IDalamudPlugin {
        private bool _disposedValue;

        public string Name => "NoSoliciting";

        private Filter Filter { get; set; } = null!;

        public DalamudPluginInterface Interface { get; private set; } = null!;
        public PluginConfiguration Config { get; private set; } = null!;
        public XivCommonBase Common { get; private set; } = null!;
        public PluginUi Ui { get; private set; } = null!;
        public Commands Commands { get; private set; } = null!;
        private ContextMenu ContextMenu { get; set; } = null!;
        public MlFilterStatus MlStatus { get; set; } = MlFilterStatus.Uninitialised;
        public MlFilter? MlFilter { get; set; }

        private readonly List<Message> _messageHistory = new();
        public IEnumerable<Message> MessageHistory => this._messageHistory;

        private readonly List<Message> _partyFinderHistory = new();
        public IEnumerable<Message> PartyFinderHistory => this._partyFinderHistory;

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string AssemblyLocation { get; private set; } = Assembly.GetExecutingAssembly().Location;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            string path = Environment.GetEnvironmentVariable("PATH")!;
            string newPath = Path.GetDirectoryName(this.AssemblyLocation)!;
            Environment.SetEnvironmentVariable("PATH", $"{path};{newPath}");

            this.Interface = pluginInterface;

            this.Config = this.Interface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            this.Config.Initialise(this.Interface);

            Util.PreloadEmbeddedResources(this.GetType().Assembly, Language.ResourceManager, "NoSoliciting.Resources.Language.");
            this.ConfigureLanguage();
            this.Interface.OnLanguageChanged += this.OnLanguageUpdate;

            this.Common = new XivCommonBase(this.Interface, Hooks.PartyFinder | Hooks.ContextMenu);

            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);
            this.ContextMenu = new ContextMenu(this);

            this.Filter = new Filter(this);

            this.InitialiseMachineLearning(false);

            // pre-compute the max ilvl to prevent stutter
            try {
                FilterUtil.MaxItemLevelAttainable(this.Interface.Data);
            } catch (Exception ex) {
                PluginLog.LogError(ex, "Exception while computing max item level");
            }
        }

        protected virtual void Dispose(bool disposing) {
            if (this._disposedValue) {
                return;
            }

            if (disposing) {
                this.Filter.Dispose();
                this.MlFilter?.Dispose();
                this.ContextMenu.Dispose();
                this.Commands.Dispose();
                this.Ui.Dispose();
                this.Common.Dispose();
                this.Interface.OnLanguageChanged -= this.OnLanguageUpdate;
            }

            this._disposedValue = true;
        }

        private void OnLanguageUpdate(string langCode) {
            this.ConfigureLanguage(langCode);
        }

        internal void ConfigureLanguage(string? langCode = null) {
            if (this.Config.FollowGameLanguage) {
                langCode = this.Interface.ClientState.ClientLanguage switch {
                    ClientLanguage.Japanese => "ja",
                    ClientLanguage.English => "en",
                    ClientLanguage.German => "de",
                    ClientLanguage.French => "fr",
                    _ => throw new ArgumentOutOfRangeException(),
                };
            }

            langCode ??= this.Interface.UiLanguage;
            Resources.Language.Culture = new CultureInfo(langCode ?? "en");
        }

        internal void InitialiseMachineLearning(bool showWindow) {
            if (this.MlFilter != null) {
                return;
            }

            Task.Run(async () => this.MlFilter = await MlFilter.Load(this, showWindow))
                .ContinueWith(e => {
                    if (e.IsFaulted) {
                        this.MlStatus = MlFilterStatus.Uninitialised;
                        return;
                    }

                    this.MlStatus = MlFilterStatus.Initialised;
                    PluginLog.Log("Machine learning model loaded");
                });
        }

        public void AddMessageHistory(Message message) {
            this._messageHistory.Insert(0, message);

            while (this._messageHistory.Count > 250) {
                this._messageHistory.RemoveAt(this._messageHistory.Count - 1);
            }
        }

        public void ClearPartyFinderHistory() {
            this._partyFinderHistory.Clear();
        }

        public void AddPartyFinderHistory(Message message) {
            this._partyFinderHistory.Add(message);
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
