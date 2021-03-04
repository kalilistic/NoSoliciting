using System;

namespace NoSoliciting.Interface {
    public class PluginUi : IDisposable {
        private Plugin Plugin { get; }

        public Settings Settings { get; }
        public Report Report { get; }

        public PluginUi(Plugin plugin) {
            this.Plugin = plugin;

            this.Settings = new Settings(plugin, this);
            this.Report = new Report(plugin);

            this.Plugin.Interface.UiBuilder.OnBuildUi += this.Draw;
        }

        public void Dispose() {
            this.Plugin.Interface.UiBuilder.OnBuildUi -= this.Draw;
            this.Settings.Dispose();
        }

        private void Draw() {
            this.Settings.Draw();
            this.Report.Draw();
        }
    }
}
