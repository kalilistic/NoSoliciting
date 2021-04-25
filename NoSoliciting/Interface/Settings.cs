using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using Dalamud.Interface;
using ImGuiNET;
using NoSoliciting.Ml;

namespace NoSoliciting.Interface {
    public class Settings : IDisposable {
        private Plugin Plugin { get; }
        private PluginUi Ui { get; }

        private bool _showSettings;

        private bool ShowSettings {
            get => this._showSettings;
            set => this._showSettings = value;
        }

        public Settings(Plugin plugin, PluginUi ui) {
            this.Plugin = plugin;
            this.Ui = ui;

            this.Plugin.Interface.UiBuilder.OnOpenConfigUi += this.Open;
        }

        public void Dispose() {
            this.Plugin.Interface.UiBuilder.OnOpenConfigUi -= this.Open;
        }

        private void Open(object? sender, EventArgs? e) {
            this.ShowSettings = true;
        }

        public void Toggle() {
            this.ShowSettings = !this.ShowSettings;
        }

        public void Draw() {
            if (!this.ShowSettings || !ImGui.Begin($"{this.Plugin.Name} settings", ref this._showSettings)) {
                return;
            }

            var modes = new[] {
                "Machine learning (default)",
                "Definition matchers (obsolete)",
            };
            var modeIndex = this.Plugin.Config.UseMachineLearning ? 0 : 1;
            if (ImGui.Combo("Filter mode", ref modeIndex, modes, modes.Length)) {
                this.Plugin.Config.UseMachineLearning = modeIndex == 0;
                this.Plugin.Config.Save();

                if (this.Plugin.Config.UseMachineLearning) {
                    this.Plugin.InitialiseMachineLearning(false);
                }
            }

            var advanced = this.Plugin.Config.AdvancedMode;
            if (ImGui.Checkbox("Advanced mode", ref advanced)) {
                this.Plugin.Config.AdvancedMode = advanced;
                this.Plugin.Config.Save();
            }

            ImGui.Spacing();

            if (!ImGui.BeginTabBar("##nosoliciting-tabs")) {
                return;
            }

            if (this.Plugin.Config.UseMachineLearning) {
                this.DrawMachineLearningConfig();
            } else {
                this.DrawDefinitionsConfig();
            }

            this.DrawOtherFilters();

            if (ImGui.BeginTabItem("Other")) {
                var logFilteredPfs = this.Plugin.Config.LogFilteredPfs;
                if (ImGui.Checkbox("Log filtered PFs", ref logFilteredPfs)) {
                    this.Plugin.Config.LogFilteredPfs = logFilteredPfs;
                    this.Plugin.Config.Save();
                }

                var logFilteredMessages = this.Plugin.Config.LogFilteredChat;
                if (ImGui.Checkbox("Log filtered messages", ref logFilteredMessages)) {
                    this.Plugin.Config.LogFilteredChat = logFilteredMessages;
                    this.Plugin.Config.Save();
                }

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();

            ImGui.Separator();

            if (ImGui.Button("Show reporting window")) {
                this.Ui.Report.Open();
            }

            ImGui.End();
        }

        #region ML config

        private void DrawMachineLearningConfig() {
            if (this.Plugin.Config.AdvancedMode) {
                this.DrawAdvancedMachineLearningConfig();
            } else {
                this.DrawBasicMachineLearningConfig();
            }

            if (!ImGui.BeginTabItem("Model")) {
                return;
            }

            ImGui.TextUnformatted($"Version: {this.Plugin.MlFilter?.Version}");
            ImGui.TextUnformatted($"Model status: {this.Plugin.MlStatus.Description()}");
            var lastError = MlFilter.LastError;
            if (lastError != null) {
                ImGui.TextUnformatted($"Last error: {lastError}");
            }

            if (ImGui.Button("Update model")) {
                // prevent issues when people spam the button
                if (ImGui.GetIO().KeyCtrl || this.Plugin.MlStatus is MlFilterStatus.Uninitialised or MlFilterStatus.Initialised) {
                    this.Plugin.MlFilter?.Dispose();
                    this.Plugin.MlFilter = null;
                    this.Plugin.MlStatus = MlFilterStatus.Uninitialised;
                    this.Plugin.InitialiseMachineLearning(ImGui.GetIO().KeyAlt);
                }
            }

            ImGui.EndTabItem();
        }

        private void DrawBasicMachineLearningConfig() {
            if (!ImGui.BeginTabItem("Filters")) {
                return;
            }

            foreach (var category in MessageCategoryExt.UiOrder) {
                var check = this.Plugin.Config.BasicMlFilters.Contains(category);
                if (ImGui.Checkbox(category.Name(), ref check)) {
                    if (check) {
                        this.Plugin.Config.BasicMlFilters.Add(category);
                    } else {
                        this.Plugin.Config.BasicMlFilters.Remove(category);
                    }

                    this.Plugin.Config.Save();
                }

                if (!ImGui.IsItemHovered()) {
                    continue;
                }

                ImGui.BeginTooltip();
                ImGui.PushTextWrapPos(ImGui.GetFontSize() * 24);
                ImGui.TextUnformatted(category.Description());
                ImGui.PopTextWrapPos();
                ImGui.EndTooltip();
            }

            ImGui.EndTabItem();
        }

        private void DrawAdvancedMachineLearningConfig() {
            if (!ImGui.BeginTabItem("Filters")) {
                return;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255f, 204f, 0f, 1f));
            ImGui.TextUnformatted("Do not change advanced settings unless you know what you are doing.");
            ImGui.TextUnformatted("The machine learning model was trained with certain channels in mind.");
            ImGui.PopStyleColor();

            foreach (var category in MessageCategoryExt.UiOrder) {
                if (!ImGui.CollapsingHeader(category.Name())) {
                    continue;
                }

                if (!this.Plugin.Config.MlFilters.ContainsKey(category)) {
                    this.Plugin.Config.MlFilters[category] = new HashSet<ChatType>();
                }

                var types = this.Plugin.Config.MlFilters[category];

                void DrawTypes(ChatType type, string id) {
                    var name = type.Name(this.Plugin.Interface.Data);

                    var check = types.Contains(type);
                    if (!ImGui.Checkbox($"{name}##{id}", ref check)) {
                        return;
                    }

                    if (check) {
                        types.Add(type);
                    } else {
                        types.Remove(type);
                    }

                    this.Plugin.Config.Save();
                }

                DrawTypes(ChatType.None, category.ToString());

                foreach (var type in Filter.FilteredChatTypes) {
                    DrawTypes(type, category.ToString());
                }
            }

            ImGui.EndTabItem();
        }

        #endregion

        #region Definitions config

        private void DrawDefinitionsConfig() {
            if (this.Plugin.Config.AdvancedMode) {
                this.DrawDefsAdvancedSettings();
            } else {
                this.DrawDefsBasicSettings();
            }

            this.DrawDefinitionsTab();
        }

        private void DrawDefinitionsTab() {
            if (!ImGui.BeginTabItem("Definitions")) {
                return;
            }

            if (this.Plugin.Definitions != null) {
                ImGui.TextUnformatted($"Version: {this.Plugin.Definitions.Version}");
            }

            if (Definitions.LastUpdate != null) {
                ImGui.TextUnformatted($"Last update: {Definitions.LastUpdate}");
            }

            var error = Definitions.LastError;
            if (error != null) {
                ImGui.TextUnformatted($"Last error: {error}");
            }

            if (ImGui.Button("Update definitions")) {
                this.Plugin.UpdateDefinitions();
            }

            ImGui.EndTabItem();
        }

        private void DrawDefsBasicSettings() {
            if (this.Plugin.Definitions == null) {
                return;
            }

            if (!ImGui.BeginTabItem("Filters")) {
                return;
            }

            this.DrawCheckboxes(this.Plugin.Definitions.Chat.Values, true, "chat");

            ImGui.Separator();

            this.DrawCheckboxes(this.Plugin.Definitions.PartyFinder.Values, true, "Party Finder");

            ImGui.EndTabItem();
        }

        private void DrawDefsAdvancedSettings() {
            if (this.Plugin.Definitions == null) {
                return;
            }

            if (ImGui.BeginTabItem("Chat")) {
                this.DrawCheckboxes(this.Plugin.Definitions.Chat.Values, false, "chat");

                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Party Finder")) {
                this.DrawCheckboxes(this.Plugin.Definitions.PartyFinder.Values, false, "Party Finder");

                ImGui.EndTabItem();
            }
        }

        #endregion

        #region Other config

        private void DrawOtherFilters() {
            if (!ImGui.BeginTabItem("Other filters")) {
                return;
            }

            if (ImGui.CollapsingHeader("Chat filters")) {
                var customChat = this.Plugin.Config.CustomChatFilter;
                if (ImGui.Checkbox("Enable custom chat filters", ref customChat)) {
                    this.Plugin.Config.CustomChatFilter = customChat;
                    this.Plugin.Config.Save();
                }

                if (this.Plugin.Config.CustomChatFilter) {
                    var substrings = this.Plugin.Config.ChatSubstrings;
                    var regexes = this.Plugin.Config.ChatRegexes;
                    this.DrawCustom("chat", ref substrings, ref regexes);
                }
            }

            if (ImGui.CollapsingHeader("Party Finder filters")) {
                var filterHugeItemLevelPFs = this.Plugin.Config.FilterHugeItemLevelPFs;
                // ReSharper disable once InvertIf
                if (ImGui.Checkbox("Filter PFs with item level above maximum", ref filterHugeItemLevelPFs)) {
                    this.Plugin.Config.FilterHugeItemLevelPFs = filterHugeItemLevelPFs;
                    this.Plugin.Config.Save();
                }

                var considerPrivate = this.Plugin.Config.ConsiderPrivatePfs;
                if (ImGui.Checkbox("Apply filters to private Party Finder listings", ref considerPrivate)) {
                    this.Plugin.Config.ConsiderPrivatePfs = considerPrivate;
                    this.Plugin.Config.Save();
                }

                var customPf = this.Plugin.Config.CustomPFFilter;
                if (ImGui.Checkbox("Enable custom Party Finder filters", ref customPf)) {
                    this.Plugin.Config.CustomPFFilter = customPf;
                    this.Plugin.Config.Save();
                }

                if (this.Plugin.Config.CustomPFFilter) {
                    var substrings = this.Plugin.Config.PFSubstrings;
                    var regexes = this.Plugin.Config.PFRegexes;
                    this.DrawCustom("pf", ref substrings, ref regexes);
                }
            }

            ImGui.EndTabItem();
        }

        private void DrawCustom(string name, ref List<string> substrings, ref List<string> regexes) {
            ImGui.Columns(2);

            ImGui.TextUnformatted("Substrings to filter");
            if (ImGui.BeginChild($"##{name}-substrings", new Vector2(0, 175))) {
                for (var i = 0; i < substrings.Count; i++) {
                    var input = substrings[i];
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

            ImGui.TextUnformatted("Regular expressions to filter");
            if (ImGui.BeginChild($"##{name}-regexes", new Vector2(0, 175))) {
                for (var i = 0; i < regexes.Count; i++) {
                    var input = regexes[i];
                    if (ImGui.InputText($"##{name}-regex-{i}", ref input, 1_000)) {
                        var valid = true;
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

            // ReSharper disable once InvertIf
            if (ImGui.Button($"Save filters##{name}-save")) {
                this.Plugin.Config.Save();
                this.Plugin.Config.CompileRegexes();
            }
        }

        #endregion

        #region Utility

        private void DrawCheckboxes(IEnumerable<Definition> defs, bool basic, string labelFillIn) {
            foreach (var def in defs) {
                this.Plugin.Config.FilterStatus.TryGetValue(def.Id, out var enabled);
                var label = basic ? def.Option.Basic : def.Option.Advanced;
                label = label.Replace("{}", labelFillIn);
                // ReSharper disable once InvertIf
                if (ImGui.Checkbox(label, ref enabled)) {
                    this.Plugin.Config.FilterStatus[def.Id] = enabled;
                    this.Plugin.Config.Save();
                }
            }
        }

        #endregion
    }
}
