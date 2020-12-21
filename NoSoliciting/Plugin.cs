using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NoSoliciting.Ml;

namespace NoSoliciting {
    public class Plugin : IDalamudPlugin {
        private bool _disposedValue;

        public string Name => "NoSoliciting";

        private PluginUi Ui { get; set; } = null!;
        private Filter Filter { get; set; } = null!;

        public DalamudPluginInterface Interface { get; private set; } = null!;
        public PluginConfiguration Config { get; private set; } = null!;
        public Definitions? Definitions { get; private set; }
        public MlFilter? MlFilter { get; set; }
        public bool MlReady => this.Config.UseMachineLearning && this.MlFilter != null;
        public bool DefsReady => !this.Config.UseMachineLearning && this.Definitions != null;

        private readonly List<Message> _messageHistory = new List<Message>();
        public IEnumerable<Message> MessageHistory => this._messageHistory;

        private readonly List<Message> _partyFinderHistory = new List<Message>();
        public IEnumerable<Message> PartyFinderHistory => this._partyFinderHistory;

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
        public string AssemblyLocation { get; private set; } = Assembly.GetExecutingAssembly().Location;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            // NOTE: THE SECRET IS TO DOWNGRADE System.Numerics.Vectors THAT'S INCLUDED WITH DALAMUD
            //       CRY

            string path = Environment.GetEnvironmentVariable("PATH")!;
            string newPath = Path.GetDirectoryName(this.AssemblyLocation)!;
            Environment.SetEnvironmentVariable("PATH", $"{path};{newPath}");

            this.Interface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");
            this.Ui = new PluginUi(this);

            this.Config = this.Interface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            this.Config.Initialise(this.Interface);

            this.UpdateDefinitions();

            this.Filter = new Filter(this);

            if (this.Config.UseMachineLearning) {
                this.InitialiseMachineLearning();
            }

            // pre-compute the max ilvl to prevent stutter
            Task.Run(async () => {
                while (!this.Interface.Data.IsDataReady) {
                    await Task.Delay(1_000).ConfigureAwait(true);
                }

                FilterUtil.MaxItemLevelAttainable(this.Interface.Data);
            });

            this.Interface.Framework.Gui.Chat.OnCheckMessageHandled += this.Filter.OnChat;
            this.Interface.UiBuilder.OnBuildUi += this.Ui.Draw;
            this.Interface.UiBuilder.OnOpenConfigUi += this.Ui.OpenSettings;
            this.Interface.CommandManager.AddHandler("/prmt", new CommandInfo(this.OnCommand) {
                HelpMessage = "Opens the NoSoliciting configuration",
            });
        }

        internal void InitialiseMachineLearning() {
            if (this.MlFilter != null) {
                return;
            }

            Task.Run(async () => { this.MlFilter = await MlFilter.Load(this); })
                .ContinueWith(_ => PluginLog.Log("Machine learning model loaded"));
        }

        internal void UpdateDefinitions() {
            Task.Run(async () => {
                var defs = await Definitions.UpdateAndCache(this).ConfigureAwait(true);
                // this shouldn't be possible, but what do I know
                if (defs != null) {
                    defs.Initialise(this);
                    this.Definitions = defs;
                    Definitions.LastUpdate = DateTime.Now;
                }
            });
        }

        private void OnCommand(string command, string args) {
            this.Ui.OpenSettings(null, null);
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

        protected virtual void Dispose(bool disposing) {
            if (this._disposedValue) {
                return;
            }

            if (disposing) {
                this.Filter.Dispose();
                this.MlFilter?.Dispose();
                this.Interface.Framework.Gui.Chat.OnCheckMessageHandled -= this.Filter.OnChat;
                this.Interface.UiBuilder.OnBuildUi -= this.Ui.Draw;
                this.Interface.UiBuilder.OnOpenConfigUi -= this.Ui.OpenSettings;
                this.Interface.CommandManager.RemoveHandler("/prmt");
            }

            this._disposedValue = true;
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
