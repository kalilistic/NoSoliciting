using ImGuiNET;
using System;

namespace NoSoliciting {
    public class PluginUI {
        private readonly Plugin plugin;

        private bool _showSettings;
        public bool ShowSettings { get => this._showSettings; set => this._showSettings = value; }

        public PluginUI(Plugin plugin) {
            this.plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin cannot be null");
        }

        public void OpenSettings(object sender, EventArgs e) {
            this.ShowSettings = true;
        }

        public void Draw() {
            if (this.ShowSettings) {
                this.DrawSettings();
            }
        }

        public void DrawSettings() {
            if (ImGui.Begin($"{this.plugin.Name} settings", ref this._showSettings)) {
                bool filterChat = this.plugin.Config.FilterChat;
                if (ImGui.Checkbox("Filter RMT from chat", ref filterChat)) {
                    this.plugin.Config.FilterChat = filterChat;
                    this.plugin.Config.Save();
                }

                bool filterPartyFinder = this.plugin.Config.FilterPartyFinder;
                if (ImGui.Checkbox("Filter RMT from Party Finder", ref filterPartyFinder)) {
                    this.plugin.Config.FilterPartyFinder = filterPartyFinder;
                    this.plugin.Config.Save();
                }

                ImGui.End();
            }
        }
    }
}
