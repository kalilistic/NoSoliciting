using Dalamud.Game.Command;
using Dalamud.Plugin;
using System;

namespace NoSoliciting {
    public partial class Plugin : IDalamudPlugin {
        private bool disposedValue;

        public string Name => "NoSoliciting";

        private DalamudPluginInterface pi;
        private PluginUI ui;
        private RMTDetection rmt;

        public PluginConfiguration Config { get; private set; }

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.pi = pluginInterface ?? throw new ArgumentNullException(nameof(pluginInterface), "DalamudPluginInterface cannot be null");
            this.ui = new PluginUI(this);

            this.Config = this.pi.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration(this.pi);

            this.rmt = new RMTDetection(this);

            this.pi.Framework.Network.OnNetworkMessage += this.rmt.OnNetwork;
            this.pi.Framework.Gui.Chat.OnCheckMessageHandled += this.rmt.OnChat;
            this.pi.UiBuilder.OnBuildUi += this.ui.Draw;
            this.pi.UiBuilder.OnOpenConfigUi += this.ui.OpenSettings;
            this.pi.CommandManager.AddHandler("/prmt", new CommandInfo(OnCommand) {
                HelpMessage = "Opens the NoSoliciting configuration"
            });
        }

        public void OnCommand(string command, string args) {
            this.ui.OpenSettings(null, null);
        }

        protected virtual void Dispose(bool disposing) {
            if (!this.disposedValue) {
                if (disposing) {
                    this.pi.Framework.Network.OnNetworkMessage -= this.rmt.OnNetwork;
                    this.pi.Framework.Gui.Chat.OnCheckMessageHandled -= this.rmt.OnChat;
                    this.pi.UiBuilder.OnBuildUi -= this.ui.Draw;
                    this.pi.UiBuilder.OnOpenConfigUi -= this.ui.OpenSettings;
                    this.pi.CommandManager.RemoveHandler("/prmt");
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
