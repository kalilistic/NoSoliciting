using Dalamud.Interface;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;

namespace NoSoliciting {
    public class PluginUI {
        private readonly Plugin plugin;
        private bool resizeWindow = false;

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
            if (this.resizeWindow) {
                this.resizeWindow = false;
                ImGui.SetNextWindowSize(new Vector2(this.plugin.Config.AdvancedMode ? 600 : 0, 0));
            } else {
                ImGui.SetNextWindowSize(new Vector2(0, 0), ImGuiCond.FirstUseEver);
            }
            if (ImGui.Begin($"{this.plugin.Name} settings", ref this._showSettings)) {
                if (this.plugin.Config.AdvancedMode) {
                    this.DrawAdvancedSettings();
                } else {
                    this.DrawBasicSettings();
                }

                ImGui.Separator();

                bool advanced = this.plugin.Config.AdvancedMode;
                if (ImGui.Checkbox("Advanced mode", ref advanced)) {
                    this.plugin.Config.AdvancedMode = advanced;
                    this.plugin.Config.Save();
                    resizeWindow = true;
                }

                ImGui.End();
            }
        }

        private void DrawBasicSettings() {
            if (this.plugin.Definitions == null) {
                return;
            }

            this.DrawCheckboxes(this.plugin.Definitions.Chat.Values, true, "chat");

            ImGui.Separator();

            this.DrawCheckboxes(this.plugin.Definitions.PartyFinder.Values, true, "Party Finder");
        }

        private void DrawAdvancedSettings() {
            if (ImGui.BeginTabBar("##nosoliciting-tabs")) {
                if (this.plugin.Definitions != null) {
                    if (ImGui.BeginTabItem("Chat")) {
                        this.DrawCheckboxes(this.plugin.Definitions.Chat.Values, false, "chat");

                        bool customChat = this.plugin.Config.CustomChatFilter;
                        if (ImGui.Checkbox("Enable custom chat filters", ref customChat)) {
                            this.plugin.Config.CustomChatFilter = customChat;
                            this.plugin.Config.Save();
                        }

                        if (this.plugin.Config.CustomChatFilter) {
                            List<string> substrings = this.plugin.Config.ChatSubstrings;
                            List<string> regexes = this.plugin.Config.ChatRegexes;
                            this.DrawCustom("chat", ref substrings, ref regexes);
                        }

                        ImGui.EndTabItem();
                    }

                    if (ImGui.BeginTabItem("Party Finder")) {
                        this.DrawCheckboxes(this.plugin.Definitions.PartyFinder.Values, false, "Party Finder");

                        bool customPF = this.plugin.Config.CustomPFFilter;
                        if (ImGui.Checkbox("Enable custom Party Finder filters", ref customPF)) {
                            this.plugin.Config.CustomPFFilter = customPF;
                            this.plugin.Config.Save();
                        }

                        if (this.plugin.Config.CustomPFFilter) {
                            List<string> substrings = this.plugin.Config.PFSubstrings;
                            List<string> regexes = this.plugin.Config.PFRegexes;
                            this.DrawCustom("pf", ref substrings, ref regexes);
                        }

                        ImGui.EndTabItem();
                    }
                }

                if (ImGui.BeginTabItem("Definitions")) {
                    if (this.plugin.Definitions != null) {
                        ImGui.Text($"Version: {this.plugin.Definitions.Version}");
                    }

                    if (Definitions.LastUpdate != null) {
                        ImGui.Text($"Last update: {Definitions.LastUpdate}");
                    }

                    string error = Definitions.LastError;
                    if (error != null) {
                        ImGui.Text($"Last error: {error}");
                    }

                    if (ImGui.Button("Update definitions")) {
                        this.plugin.UpdateDefinitions();
                    }
                }

                ImGui.EndTabBar();
            }
        }

        private void DrawCustom(string name, ref List<string> substrings, ref List<string> regexes) {
            ImGui.Columns(2);

            ImGui.Text("Substrings to filter");
            if (ImGui.BeginChild($"##{name}-substrings", new Vector2(0, 175))) {
                for (int i = 0; i < substrings.Count; i++) {
                    string input = substrings[i];
                    if (ImGui.InputText($"##{name}-substring-{i}", ref input, 1_000)) {
                        if (input.Length != 0) {
                            substrings[i] = input;
                        }
                    }
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##{name}-substring-{i}-remove")) {
                        substrings.RemoveAt(i);
                    }
                    ImGui.PopFont();
                }

                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}##{name}-substring-add")) {
                    substrings.Add("");
                }
                ImGui.PopFont();

                ImGui.EndChild();
            }

            ImGui.NextColumn();

            ImGui.Text("Regular expressions to filter");
            if (ImGui.BeginChild($"##{name}-regexes", new Vector2(0, 175))) {
                for (int i = 0; i < regexes.Count; i++) {
                    string input = regexes[i];
                    if (ImGui.InputText($"##{name}-regex-{i}", ref input, 1_000)) {
                        bool valid = true;
                        try {
                            _ = new Regex(input);
                        } catch (ArgumentException) {
                            valid = false;
                        }
                        if (valid && input.Length != 0) {
                            regexes[i] = input;
                        }
                    }
                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##{name}-regex-{i}-remove")) {
                        regexes.RemoveAt(i);
                    }
                    ImGui.PopFont();
                }

                ImGui.PushFont(UiBuilder.IconFont);
                if (ImGui.Button($"{FontAwesomeIcon.Plus.ToIconString()}##{name}-regex-add")) {
                    regexes.Add("");
                }
                ImGui.PopFont();

                ImGui.EndChild();
            }

            ImGui.Columns(1);

            if (ImGui.Button($"Save filters##{name}-save")) {
                this.plugin.Config.Save();
            }
        }

        private void DrawCheckboxes(IEnumerable<Definition> defs, bool basic, string labelFillIn) {
            foreach (Definition def in defs) {
                this.plugin.Config.FilterStatus.TryGetValue(def.Id, out bool enabled);
                string label = basic ? def.Option.Basic : def.Option.Advanced;
                label = label.Replace("{}", labelFillIn);
                if (ImGui.Checkbox(label, ref enabled)) {
                    this.plugin.Config.FilterStatus[def.Id] = enabled;
                    this.plugin.Config.Save();
                }
            }
        }
    }
}
