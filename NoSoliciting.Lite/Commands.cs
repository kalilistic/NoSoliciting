using System;
using Dalamud.Game.Command;

namespace NoSoliciting.Lite {
    public class Commands : IDisposable {
        private Plugin Plugin { get; }

        internal Commands(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.CommandManager.AddHandler("/nolite", new CommandInfo(this.OnCommand) {
                HelpMessage = "Open the NoSol Lite config",
            });
        }

        public void Dispose() {
            this.Plugin.Interface.CommandManager.RemoveHandler("/nolite");
        }

        private void OnCommand(string command, string args) {
            this.Plugin.Ui.ToggleConfig();
        }
    }
}
