using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text.RegularExpressions;
using CheapLoc;
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
            var windowTitle = string.Format(Loc.Localize("Settings", "{0} settings"), this.Plugin.Name);
            if (!this.ShowSettings || !ImGui.Begin($"{windowTitle}###NoSoliciting settings", ref this._showSettings)) {
                return;
            }

            var advanced = this.Plugin.Config.AdvancedMode;
            if (ImGui.Checkbox(Loc.Localize("AdvancedMode", "Advanced mode"), ref advanced)) {
                this.Plugin.Config.AdvancedMode = advanced;
                this.Plugin.Config.Save();
            }

            ImGui.Spacing();

            if (!ImGui.BeginTabBar("##nosoliciting-tabs")) {
                return;
            }

            this.DrawMachineLearningConfig();

            this.DrawOtherFilters();

            if (ImGui.BeginTabItem(Loc.Localize("OtherTab", "Other"))) {
                var logFilteredPfs = this.Plugin.Config.LogFilteredPfs;
                if (ImGui.Checkbox(Loc.Localize("LogFilteredPfs", "Log filtered PFs"), ref logFilteredPfs)) {
                    this.Plugin.Config.LogFilteredPfs = logFilteredPfs;
                    this.Plugin.Config.Save();
                }

                var logFilteredMessages = this.Plugin.Config.LogFilteredChat;
                if (ImGui.Checkbox(Loc.Localize("LogFilteredMessages", "Log filtered messages"), ref logFilteredMessages)) {
                    this.Plugin.Config.LogFilteredChat = logFilteredMessages;
                    this.Plugin.Config.Save();
                }

                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();

            ImGui.Separator();

            if (ImGui.Button(Loc.Localize("ShowReportingWindow", "Show reporting window"))) {
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

            if (!ImGui.BeginTabItem(Loc.Localize("ModelTab", "Model"))) {
                return;
            }

            ImGui.TextUnformatted(string.Format(Loc.Localize("ModelTabVersion", "Version: {0}"), this.Plugin.MlFilter?.Version));
            ImGui.TextUnformatted(string.Format(Loc.Localize("ModelTabStatus", "Model status: {0}"), this.Plugin.MlStatus.Description()));
            var lastError = MlFilter.LastError;
            if (lastError != null) {
                ImGui.TextUnformatted(string.Format(Loc.Localize("ModelTabError", "Last error: {0}"), lastError));
            }

            if (ImGui.Button(Loc.Localize("UpdateModel", "Update model"))) {
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
            if (!ImGui.BeginTabItem(Loc.Localize("FiltersTab", "Filters"))) {
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
            if (!ImGui.BeginTabItem(Loc.Localize("FiltersTab", "Filters"))) {
                return;
            }

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(255f, 204f, 0f, 1f));
            ImGui.TextUnformatted(Loc.Localize("AdvancedWarning1", "Do not change advanced settings unless you know what you are doing."));
            ImGui.TextUnformatted(Loc.Localize("AdvancedWarning2", "The machine learning model was trained with certain channels in mind."));
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

        #region Other config

        private void DrawOtherFilters() {
            if (!ImGui.BeginTabItem(Loc.Localize("OtherFiltersTab", "Other filters"))) {
                return;
            }

            if (ImGui.CollapsingHeader(Loc.Localize("ChatFilters", "Chat filters"))) {
                var customChat = this.Plugin.Config.CustomChatFilter;
                if (ImGui.Checkbox(Loc.Localize("EnableCustomChatFilters", "Enable custom chat filters"), ref customChat)) {
                    this.Plugin.Config.CustomChatFilter = customChat;
                    this.Plugin.Config.Save();
                }

                if (this.Plugin.Config.CustomChatFilter) {
                    var substrings = this.Plugin.Config.ChatSubstrings;
                    var regexes = this.Plugin.Config.ChatRegexes;
                    this.DrawCustom("chat", ref substrings, ref regexes);
                }
            }

            if (ImGui.CollapsingHeader(Loc.Localize("PartyFinderFilters", "Party Finder filters"))) {
                var filterHugeItemLevelPFs = this.Plugin.Config.FilterHugeItemLevelPFs;
                // ReSharper disable once InvertIf
                if (ImGui.Checkbox(Loc.Localize("FilterIlvlPfs", "Filter PFs with item level above maximum"), ref filterHugeItemLevelPFs)) {
                    this.Plugin.Config.FilterHugeItemLevelPFs = filterHugeItemLevelPFs;
                    this.Plugin.Config.Save();
                }

                var considerPrivate = this.Plugin.Config.ConsiderPrivatePfs;
                if (ImGui.Checkbox(Loc.Localize("FilterPrivatePfs", "Apply filters to private Party Finder listings"), ref considerPrivate)) {
                    this.Plugin.Config.ConsiderPrivatePfs = considerPrivate;
                    this.Plugin.Config.Save();
                }

                var customPf = this.Plugin.Config.CustomPFFilter;
                if (ImGui.Checkbox(Loc.Localize("EnableCustomPartyFinderFilters", "Enable custom Party Finder filters"), ref customPf)) {
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

            ImGui.TextUnformatted(Loc.Localize("SubstringsToFilter", "Substrings to filter"));
            if (ImGui.BeginChild($"##{name}-substrings", new Vector2(0, 175))) {
                for (var i = 0; i < substrings.Count; i++) {
                    var input = substrings[i];
                    if (ImGui.InputText($"##{name}-substring-{i}", ref input, 1_000)) {
                        substrings[i] = input;
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

            ImGui.TextUnformatted(Loc.Localize("RegularExpressionsToFilter", "Regular expressions to filter"));
            if (ImGui.BeginChild($"##{name}-regexes", new Vector2(0, 175))) {
                for (var i = 0; i < regexes.Count; i++) {
                    var input = regexes[i];
                    if (ImGui.InputText($"##{name}-regex-{i}", ref input, 1_000)) {
                        try {
                            _ = new Regex(input);
                            // update if valid
                            regexes[i] = input;
                        } catch (ArgumentException) {
                            // ignore
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
            var saveLoc = Loc.Localize("SaveFilters", "Save filters");
            if (ImGui.Button($"{saveLoc}##{name}-save")) {
                this.Plugin.Config.Save();
                this.Plugin.Config.CompileRegexes();
            }
        }

        #endregion
    }
}
