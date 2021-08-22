using System;
using Dalamud.Game.Command;

namespace NoSoliciting {
    public class Commands : IDisposable {
        private Plugin Plugin { get; }

        internal Commands(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.CommandManager.AddHandler("/prmt", new CommandInfo(this.OnCommand) {
                HelpMessage = "Opens the NoSoliciting configuration (deprecated)",
                ShowInHelp = false,
            });
            this.Plugin.CommandManager.AddHandler("/nosol", new CommandInfo(this.OnCommand) {
                HelpMessage = "Opens the NoSoliciting configuration",
            });
        }

        public void Dispose() {
            this.Plugin.CommandManager.RemoveHandler("/nosol");
            this.Plugin.CommandManager.RemoveHandler("/prmt");
        }

        private void OnCommand(string command, string args) {
            if (command == "/prmt") {
                this.Plugin.ChatGui.PrintError($"[{this.Plugin.Name}] The /prmt command is deprecated and will be removed. Please use /nosol instead.");
            }

            if (args == "report") {
                this.Plugin.Ui.Report.Toggle();
                return;
            }

            this.Plugin.Ui.Settings.Toggle();
        }
    }
}
