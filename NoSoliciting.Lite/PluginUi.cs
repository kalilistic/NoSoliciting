using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface;
using ImGuiNET;
using NoSoliciting.Lite.Resources;

namespace NoSoliciting.Lite {
    public class PluginUi : IDisposable {
        private Plugin Plugin { get; }

        private bool _showWindow;

        internal PluginUi(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Interface.UiBuilder.OnBuildUi += this.Draw;
            this.Plugin.Interface.UiBuilder.OnOpenConfigUi += this.ToggleConfig;
        }

        public void Dispose() {
            this.Plugin.Interface.UiBuilder.OnOpenConfigUi -= this.ToggleConfig;
            this.Plugin.Interface.UiBuilder.OnBuildUi -= this.Draw;
        }

        internal void ToggleConfig(object? sender = null, object? args = null) {
            this._showWindow = !this._showWindow;
        }

        private void Draw() {
            if (!this._showWindow) {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(550, 350), ImGuiCond.FirstUseEver);

            var windowTitle = string.Format(Language.Settings, this.Plugin.Name);
            if (!ImGui.Begin($"{windowTitle}###nosol-lite-settings", ref this._showWindow)) {
                ImGui.End();
                return;
            }

            var shouldSave = false;

            if (ImGui.BeginTabBar("nosol-lite-tabs")) {
                if (ImGui.BeginTabItem("Chat")) {
                    var customChat = this.Plugin.Config.CustomChatFilter;
                    if (ImGui.Checkbox(Language.EnableCustomChatFilters, ref customChat)) {
                        this.Plugin.Config.CustomChatFilter = customChat;
                        shouldSave = true;
                    }

                    if (this.Plugin.Config.CustomChatFilter) {
                        var substrings = this.Plugin.Config.ChatSubstrings;
                        var regexes = this.Plugin.Config.ChatRegexes;
                        this.DrawCustom("chat", ref shouldSave, ref substrings, ref regexes);
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Party Finder")) {
                    var considerPrivate = this.Plugin.Config.ConsiderPrivatePfs;
                    if (ImGui.Checkbox(Language.FilterPrivatePfs, ref considerPrivate)) {
                        this.Plugin.Config.ConsiderPrivatePfs = considerPrivate;
                        shouldSave = true;
                    }

                    var customPf = this.Plugin.Config.CustomPFFilter;
                    if (ImGui.Checkbox(Language.EnableCustomPartyFinderFilters, ref customPf)) {
                        this.Plugin.Config.CustomPFFilter = customPf;
                        shouldSave = true;
                    }

                    if (this.Plugin.Config.CustomPFFilter) {
                        var substrings = this.Plugin.Config.PFSubstrings;
                        var regexes = this.Plugin.Config.PFRegexes;
                        this.DrawCustom("pf", ref shouldSave, ref substrings, ref regexes);
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();

            if (!shouldSave) {
                return;
            }

            this.Plugin.Config.Save();
            this.Plugin.Config.CompileRegexes();
        }

        private void DrawCustom(string name, ref bool shouldSave, ref List<string> substrings, ref List<string> regexes) {
            ImGui.Columns(2);

            ImGui.TextUnformatted(Language.SubstringsToFilter);
            if (ImGui.BeginChild($"##{name}-substrings", new Vector2(0, 175))) {
                for (var i = 0; i < substrings.Count; i++) {
                    var input = substrings[i];
                    if (ImGui.InputText($"##{name}-substring-{i}", ref input, 1_000)) {
                        substrings[i] = input;
                        shouldSave = true;
                    }

                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##{name}-substring-{i}-remove")) {
                        substrings.RemoveAt(i);
                        shouldSave = true;
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

            ImGui.TextUnformatted(Language.RegularExpressionsToFilter);
            if (ImGui.BeginChild($"##{name}-regexes", new Vector2(0, 175))) {
                for (var i = 0; i < regexes.Count; i++) {
                    var input = regexes[i];
                    if (ImGui.InputText($"##{name}-regex-{i}", ref input, 1_000)) {
                        try {
                            _ = new Regex(input);
                            // update if valid
                            regexes[i] = input;
                            shouldSave = true;
                        } catch (ArgumentException) {
                            // ignore
                        }
                    }

                    ImGui.SameLine();
                    ImGui.PushFont(UiBuilder.IconFont);
                    if (ImGui.Button($"{FontAwesomeIcon.Trash.ToIconString()}##{name}-regex-{i}-remove")) {
                        regexes.RemoveAt(i);
                        shouldSave = true;
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
        }
    }
}
