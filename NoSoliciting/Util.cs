using System;
using System.IO;
using Dalamud.Plugin;

namespace NoSoliciting {
    public static class Util {
        public static string PluginFolder(IDalamudPlugin plugin) {
            return Path.Combine(new[] {
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "XIVLauncher",
                "pluginConfigs",
                plugin.Name,
            });
        }
    }
}
