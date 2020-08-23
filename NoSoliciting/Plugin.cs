using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;
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

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.Interface = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");
            this.ui = new PluginUI(this);

            this.Config = this.Interface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();
            this.Config.Initialise(this.Interface);

            this.UpdateDefinitions();

            this.filter = new Filter(this);

            this.Interface.Framework.Network.OnNetworkMessage += this.filter.OnNetwork;
            this.Interface.Framework.Gui.Chat.OnCheckMessageHandled += this.filter.OnChat;
            this.Interface.UiBuilder.OnBuildUi += this.ui.Draw;
            this.Interface.UiBuilder.OnOpenConfigUi += this.ui.OpenSettings;
            this.Interface.CommandManager.AddHandler("/prmt", new CommandInfo(OnCommand) {
                HelpMessage = "Opens the NoSoliciting configuration"
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

        protected virtual void Dispose(bool disposing) {
            if (!this.disposedValue) {
                if (disposing) {
                    this.Interface.Framework.Network.OnNetworkMessage -= this.filter.OnNetwork;
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
