using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;

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
                }

                ImGui.End();
            }
        }

        private void DrawBasicSettings() {
            ImGui.SetWindowSize(new Vector2(225, 125));

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
        }

        private void DrawAdvancedSettings() {
            ImGui.SetWindowSize(new Vector2(600, 400));

            if (ImGui.BeginTabBar("##nosoliciting-tabs")) {
                if (ImGui.BeginTabItem("Chat")) {
                    bool filterChat = this.plugin.Config.FilterChat;
                    if (ImGui.Checkbox("Enable built-in RMT filter", ref filterChat)) {
                        this.plugin.Config.FilterChat = filterChat;
                        this.plugin.Config.Save();
                    }

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
                    bool filterPartyFinder = this.plugin.Config.FilterPartyFinder;
                    if (ImGui.Checkbox("Enable built-in Party Finder RMT filter", ref filterPartyFinder)) {
                        this.plugin.Config.FilterPartyFinder = filterPartyFinder;
                        this.plugin.Config.Save();
                    }

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

                ImGui.EndTabBar();
            }
        }

        private void DrawCustom(string name, ref List<string> substrings, ref List<string> regexes) {
            ImGui.Columns(2);

            ImGui.Text("Substrings to filter");
            if (ImGui.BeginChild($"##{name}-substrings", new Vector2(0, 175))) {
                for (int i = 0; i < substrings.Count; i++) {
                    string input = substrings[i];
                    if (ImGui.InputText($"##{name}-substring-{i}", ref input, 100)) {
                        if (input.Length != 0) {
                            substrings[i] = input;
                        }
                    }
                    ImGui.SameLine();
                    if (ImGui.Button($"Remove##{name}-substring-{i}-remove")) {
                        substrings.RemoveAt(i);
                    }
                }

                if (ImGui.Button($"Add##{name}-substring-add")) {
                    substrings.Add("");
                }

                ImGui.EndChild();
            }

            ImGui.NextColumn();

            ImGui.Text("Regular expressions to filter");
            if (ImGui.BeginChild($"##{name}-regexes", new Vector2(0, 175))) {
                for (int i = 0; i < regexes.Count; i++) {
                    string input = regexes[i];
                    if (ImGui.InputText($"##{name}-regex-{i}", ref input, 100)) {
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
                    if (ImGui.Button($"Remove##{name}-regex-{i}-remove")) {
                        regexes.RemoveAt(i);
                    }
                }

                if (ImGui.Button($"Add##{name}-regex-add")) {
                    regexes.Add("");
                }

                ImGui.EndChild();
            }

            ImGui.Columns(1);

            if (ImGui.Button($"Save filters##{name}-save")) {
                this.plugin.Config.Save();
            }
        }
    }
}
