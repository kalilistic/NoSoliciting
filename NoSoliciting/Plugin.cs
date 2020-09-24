using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NoSoliciting {
    public partial class Plugin : IDalamudPlugin {
        private bool disposedValue;

        public string Name => "NoSoliciting";

        private PluginUI ui;
        private Filter filter;

        public DalamudPluginInterface Interface { get; private set; }
        public PluginConfiguration Config { get; private set; }
        public Definitions Definitions { get; private set; }

        private readonly List<Message> messageHistory = new List<Message>();
        public IReadOnlyCollection<Message> MessageHistory { get => this.messageHistory; }

        private readonly List<Message> partyFinderHistory = new List<Message>();
        public IReadOnlyCollection<Message> PartyFinderHistory { get => this.partyFinderHistory; }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.Interface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");
            this.ui = new PluginUI(this);

            this.Config = this.Interface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            this.Config.Initialise(this.Interface);

            this.UpdateDefinitions();

            this.filter = new Filter(this);

            // pre-compute the max ilvl to prevent stutter
            Task.Run(async () => {
                while (!this.Interface.Data.IsDataReady) {
                    await Task.Delay(1_000).ConfigureAwait(true);
                }
                FilterUtil.MaxItemLevelAttainable(this.Interface.Data);
            });

            this.Interface.Framework.Gui.Chat.OnCheckMessageHandled += this.filter.OnChat;
            this.Interface.UiBuilder.OnBuildUi += this.ui.Draw;
            this.Interface.UiBuilder.OnOpenConfigUi += this.ui.OpenSettings;
            this.Interface.CommandManager.AddHandler("/prmt", new CommandInfo(OnCommand) {
                HelpMessage = "Opens the NoSoliciting configuration",
            });
        }

        internal void UpdateDefinitions() {
            Task.Run(async () => {
                Definitions defs = await Definitions.UpdateAndCache(this).ConfigureAwait(true);
                // this shouldn't be possible, but what do I know
                if (defs != null) {
                    defs.Initialise(this);
                    this.Definitions = defs;
                    Definitions.LastUpdate = DateTime.Now;
                }
            });
        }

        public void OnCommand(string command, string args) {
            this.ui.OpenSettings(null, null);
        }

        public void AddMessageHistory(Message message) {
            this.messageHistory.Insert(0, message);

            while (this.messageHistory.Count > 250) {
                this.messageHistory.RemoveAt(this.messageHistory.Count - 1);
            }
        }

        public void ClearPartyFinderHistory() {
            this.partyFinderHistory.Clear();
        }

        public void AddPartyFinderHistory(Message message) {
            this.partyFinderHistory.Add(message);
        }

        protected virtual void Dispose(bool disposing) {
            if (!this.disposedValue) {
                if (disposing) {
                    this.filter.Dispose();
                    this.Interface.Framework.Gui.Chat.OnCheckMessageHandled -= this.filter.OnChat;
                    this.Interface.UiBuilder.OnBuildUi -= this.ui.Draw;
                    this.Interface.UiBuilder.OnOpenConfigUi -= this.ui.OpenSettings;
                    this.Interface.CommandManager.RemoveHandler("/prmt");
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        public void Dispose() {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
