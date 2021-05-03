using System.Globalization;
using Dalamud.Plugin;
using NoSoliciting.Lite.Resources;

namespace NoSoliciting.Lite {
    // ReSharper disable once ClassNeverInstantiated.Global
    public class Plugin : IDalamudPlugin {
        public string Name => "NoSoliciting Lite";

        internal DalamudPluginInterface Interface { get; private set; } = null!;
        internal Configuration Config { get; private set; } = null!;
        internal PluginUi Ui { get; private set; } = null!;
        private Commands Commands { get; set; } = null!;
        private Filter Filter { get; set; } = null!;

        public void Initialize(DalamudPluginInterface pluginInterface) {
            this.Interface = pluginInterface;

            this.ConfigureLanguage();
            this.Interface.OnLanguageChanged += this.ConfigureLanguage;

            this.Config = this.Interface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Config.Initialise(this.Interface);

            this.Filter = new Filter(this);
            this.Ui = new PluginUi(this);
            this.Commands = new Commands(this);
        }

        public void Dispose() {
            this.Commands.Dispose();
            this.Ui.Dispose();
            this.Filter.Dispose();
            this.Interface.OnLanguageChanged -= this.ConfigureLanguage;
        }

        private void ConfigureLanguage(string? langCode = null) {
            langCode ??= this.Interface.UiLanguage;
            Language.Culture = new CultureInfo(langCode ?? "en");
        }
    }
}
