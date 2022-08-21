using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Dalamud;
using Dalamud.ContextMenu;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.Command;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.PartyFinder;
using Dalamud.Game.Gui.Toast;
using Dalamud.IoC;
using Dalamud.Logging;
using NoSoliciting.Interface;
using NoSoliciting.Ml;
using NoSoliciting.Resources;
using XivCommon;

namespace NoSoliciting {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Plugin : IDalamudPlugin {
        private bool _disposedValue;

        public string Name => "NoSoliciting";

        private Filter Filter { get; }

        [PluginService]
        internal DalamudPluginInterface Interface { get; init; } = null!;

        [PluginService]
        private ClientState ClientState { get; init; } = null!;

        [PluginService]
        internal ChatGui ChatGui { get; init; } = null!;

        [PluginService]
        internal PartyFinderGui PartyFinderGui { get; init; } = null!;

        [PluginService]
        internal DataManager DataManager { get; init; } = null!;

        [PluginService]
        internal CommandManager CommandManager { get; init; } = null!;

        [PluginService]
        internal ToastGui ToastGui { get; init; } = null!;

        internal PluginConfiguration Config { get; }
        internal XivCommonBase Common { get; }
        internal DalamudContextMenu DalamudContextMenu { get; }
        internal PluginUi Ui { get; }
        private Commands Commands { get; }
        private ContextMenu ContextMenu { get; }
        internal MlFilterStatus MlStatus { get; set; } = MlFilterStatus.Uninitialised;
        internal MlFilter? MlFilter { get; set; }

        private readonly List<Message> _messageHistory = new();
        internal IEnumerable<Message> MessageHistory => this._messageHistory;

        private readonly List<Message> _partyFinderHistory = new();
        internal IEnumerable<Message> PartyFinderHistory => this._partyFinderHistory;

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string AssemblyLocation { get; private set; } = Assembly.GetExecutingAssembly().Location;

        public Plugin() {
            string path = Environment.GetEnvironmentVariable("PATH")!;
            string newPath = Path.GetDirectoryName(this.AssemblyLocation)!;
            Environment.SetEnvironmentVariable("PATH", $"{path};{newPath}");

            this.Config = this.Interface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            this.Config.Initialise(this.Interface);

            this.ConfigureLanguage();
            this.Interface.LanguageChanged += this.OnLanguageUpdate;

            this.Common = new XivCommonBase(Hooks.PartyFinderListings);
            this.DalamudContextMenu = new DalamudContextMenu();

            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);
            this.ContextMenu = new ContextMenu(this);

            this.Filter = new Filter(this);

            this.InitialiseMachineLearning(false);

            // pre-compute the max ilvl to prevent stutter
            try {
                FilterUtil.MaxItemLevelAttainable(this.DataManager);
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
                this.DalamudContextMenu.Dispose();
                this.Common.Dispose();
                this.Interface.LanguageChanged -= this.OnLanguageUpdate;
            }

            this._disposedValue = true;
        }

        private void OnLanguageUpdate(string langCode) {
            this.ConfigureLanguage(langCode);
        }

        internal void ConfigureLanguage(string? langCode = null) {
            if (this.Config.FollowGameLanguage) {
                langCode = this.ClientState.ClientLanguage switch {
                    ClientLanguage.Japanese => "ja",
                    ClientLanguage.English => "en",
                    ClientLanguage.German => "de",
                    ClientLanguage.French => "fr",
                    _ => throw new ArgumentOutOfRangeException(nameof(this.ClientState.ClientLanguage), "Unknown ClientLanguage"),
                };
            }

            langCode ??= this.Interface.UiLanguage;
            // I don't fucking trust this. Not since last time.
            // ReSharper disable once ConstantNullCoalescingCondition
            Language.Culture = new CultureInfo(langCode ?? "en");
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
