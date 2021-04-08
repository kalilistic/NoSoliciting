using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using NoSoliciting.Interface;
using NoSoliciting.Ml;

namespace NoSoliciting {
    public class Plugin : IDalamudPlugin {
        private bool _disposedValue;

        public string Name => "NoSoliciting";

        private Filter Filter { get; set; } = null!;

        public DalamudPluginInterface Interface { get; private set; } = null!;
        public PluginConfiguration Config { get; private set; } = null!;
        public PluginUi Ui { get; private set; } = null!;
        public Commands Commands { get; private set; } = null!;
        public Definitions? Definitions { get; private set; }
        public MlFilterStatus MlStatus { get; set; } = MlFilterStatus.Uninitialised;
        public MlFilter? MlFilter { get; set; }
        public bool MlReady => this.Config.UseMachineLearning && this.MlFilter != null;
        public bool DefsReady => !this.Config.UseMachineLearning && this.Definitions != null;

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

            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);

            this.UpdateDefinitions();

            this.Filter = new Filter(this);

            if (this.Config.UseMachineLearning) {
                this.InitialiseMachineLearning(false);
            }

            // pre-compute the max ilvl to prevent stutter
            FilterUtil.MaxItemLevelAttainable(this.Interface.Data);
        }

        protected virtual void Dispose(bool disposing) {
            if (this._disposedValue) {
                return;
            }

            if (disposing) {
                this.Filter.Dispose();
                this.MlFilter?.Dispose();
                this.Commands.Dispose();
                this.Ui.Dispose();
            }

            this._disposedValue = true;
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
