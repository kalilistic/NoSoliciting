using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Dalamud.Interface;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NoSoliciting {
    public class PluginUi {
        private Plugin Plugin { get; }
        private bool _resizeWindow;
        private ReportStatus LastReportStatus { get; set; } = ReportStatus.None;

        private bool _showSettings;

        private bool ShowSettings {
            get => this._showSettings;
            set => this._showSettings = value;
        }

        private bool _showReporting;

        private bool ShowReporting {
            get => this._showReporting;
            set => this._showReporting = value;
        }

        public PluginUi(Plugin plugin) {
            this.Plugin = plugin ?? throw new ArgumentNullException(nameof(plugin), "Plugin cannot be null");
        }

        public void OpenSettings(object sender, EventArgs e) {
            this.ShowSettings = true;
        }

        public void Draw() {
            if (this.ShowSettings) {
                this.DrawSettings();
            }

            if (this.ShowReporting) {
                this.DrawReportWindow();
            }
        }

        private void DrawSettings() {
            if (this._resizeWindow) {
                this._resizeWindow = false;
                ImGui.SetNextWindowSize(new Vector2(this.Plugin.Config.AdvancedMode ? 650 : 0, 0));
            } else {
                ImGui.SetNextWindowSize(new Vector2(0, 0), ImGuiCond.FirstUseEver);
            }

            if (!ImGui.Begin($"{this.Plugin.Name} settings", ref this._showSettings)) {
                return;
            }

            if (this.Plugin.Config.AdvancedMode) {
                this.DrawAdvancedSettings();
            } else {
                this.DrawBasicSettings();
            }

            ImGui.Separator();

            var advanced = this.Plugin.Config.AdvancedMode;
            if (ImGui.Checkbox("Advanced mode", ref advanced)) {
                this.Plugin.Config.AdvancedMode = advanced;
                this.Plugin.Config.Save();
                this._resizeWindow = true;
            }

            ImGui.SameLine();

            if (ImGui.Button("Show reporting window")) {
                this.ShowReporting = true;
            }

            ImGui.End();
        }

        private void DrawBasicSettings() {
            if (this.Plugin.Definitions == null) {
                return;
            }

            this.DrawCheckboxes(this.Plugin.Definitions.Chat.Values, true, "chat");

            ImGui.Separator();

            this.DrawCheckboxes(this.Plugin.Definitions.PartyFinder.Values, true, "Party Finder");

            var filterHugeItemLevelPFs = this.Plugin.Config.FilterHugeItemLevelPFs;
            // ReSharper disable once InvertIf
            if (ImGui.Checkbox("Filter PFs with item level above maximum", ref filterHugeItemLevelPFs)) {
                this.Plugin.Config.FilterHugeItemLevelPFs = filterHugeItemLevelPFs;
                this.Plugin.Config.Save();
            }
        }

        private void DrawAdvancedSettings() {
            if (!ImGui.BeginTabBar("##nosoliciting-tabs")) {
                return;
            }

            if (this.Plugin.Definitions != null) {
                if (ImGui.BeginTabItem("Chat")) {
                    this.DrawCheckboxes(this.Plugin.Definitions.Chat.Values, false, "chat");

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

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Party Finder")) {
                    this.DrawCheckboxes(this.Plugin.Definitions.PartyFinder.Values, false, "Party Finder");

                    var filterHugeItemLevelPFs = this.Plugin.Config.FilterHugeItemLevelPFs;
                    if (ImGui.Checkbox("Enable built-in maximum item level filter", ref filterHugeItemLevelPFs)) {
                        this.Plugin.Config.FilterHugeItemLevelPFs = filterHugeItemLevelPFs;
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

                    ImGui.EndTabItem();
                }
            }

            if (ImGui.BeginTabItem("Definitions")) {
                if (this.Plugin.Definitions != null) {
                    ImGui.Text($"Version: {this.Plugin.Definitions.Version}");
                }

                if (Definitions.LastUpdate != null) {
                    ImGui.Text($"Last update: {Definitions.LastUpdate}");
                }

                var error = Definitions.LastError;
                if (error != null) {
                    ImGui.Text($"Last error: {error}");
                }

                if (ImGui.Button("Update definitions")) {
                    this.Plugin.UpdateDefinitions();
                }
            }

            ImGui.EndTabBar();
        }

        private void DrawCustom(string name, ref List<string> substrings, ref List<string> regexes) {
            ImGui.Columns(2);

            ImGui.Text("Substrings to filter");
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

            ImGui.Text("Regular expressions to filter");
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

        private void DrawReportWindow() {
            ImGui.SetNextWindowSize(new Vector2(1_000, 350), ImGuiCond.FirstUseEver);

            if (!ImGui.Begin("NoSoliciting reporting", ref this._showReporting)) {
                return;
            }

            ImGui.Text("Click on one of the entries below to report it to the developer as miscategorised.");

            if (this.LastReportStatus != ReportStatus.None) {
                var status = this.LastReportStatus switch {
                    ReportStatus.Failure => "failed to send",
                    ReportStatus.Successful => "sent successfully",
                    ReportStatus.InProgress => "sending",
                    _ => "unknown",
                };
                ImGui.Text($"Last report status: {status}");
            }

            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.BeginTabBar("##report-tabs")) {
                if (ImGui.BeginTabItem("Chat##chat-report")) {
                    float[] maxSizes = {0f, 0f, 0f, 0f};

                    if (ImGui.BeginChild("##chat-messages", new Vector2(-1, -1))) {
                        ImGui.Columns(5);

                        AddColumn(maxSizes, "Timestamp", "Channel", "Reason", "Sender", "Message");
                        ImGui.Separator();

                        foreach (var message in this.Plugin.MessageHistory) {
                            if (message.FilterReason != null) {
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(238f / 255f, 71f / 255f, 71f / 255f, 1f));
                            }

                            var sender = message.Sender.Payloads
                                .Where(payload => payload.Type == PayloadType.RawText)
                                .Cast<TextPayload>()
                                .Select(payload => payload.Text)
                                .FirstOrDefault() ?? "";

                            if (AddColumn(maxSizes, message.Timestamp.ToString(CultureInfo.CurrentCulture), message.ChatType.ToString(), message.FilterReason ?? "", sender, message.Content.TextValue)) {
                                ImGui.OpenPopup($"###modal-message-{message.Id}");
                            }

                            if (message.FilterReason != null) {
                                ImGui.PopStyleColor();
                            }

                            this.SetUpReportModal(message);
                        }

                        for (var idx = 0; idx < maxSizes.Length; idx++) {
                            ImGui.SetColumnWidth(idx, maxSizes[idx] + ImGui.GetStyle().ItemSpacing.X * 2);
                        }

                        ImGui.Columns(1);

                        ImGui.EndChild();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Party Finder##pf-report")) {
                    float[] maxSizes = {0f, 0f, 0f};

                    if (ImGui.BeginChild("##pf-messages", new Vector2(-1, -1))) {
                        ImGui.Columns(4);

                        AddColumn(maxSizes, "Timestamp", "Reason", "Host", "Description");
                        ImGui.Separator();

                        foreach (var message in this.Plugin.PartyFinderHistory) {
                            if (message.FilterReason != null) {
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(238f / 255f, 71f / 255f, 71f / 255f, 1f));
                            }

                            var sender = message.Sender.Payloads
                                .Where(payload => payload.Type == PayloadType.RawText)
                                .Cast<TextPayload>()
                                .Select(payload => payload.Text)
                                .FirstOrDefault() ?? "";

                            if (AddColumn(maxSizes, message.Timestamp.ToString(CultureInfo.CurrentCulture), message.FilterReason ?? "", sender, message.Content.TextValue)) {
                                ImGui.OpenPopup($"###modal-message-{message.Id}");
                            }

                            if (message.FilterReason != null) {
                                ImGui.PopStyleColor();
                            }

                            this.SetUpReportModal(message);
                        }

                        for (var idx = 0; idx < maxSizes.Length; idx++) {
                            ImGui.SetColumnWidth(idx, maxSizes[idx] + ImGui.GetStyle().ItemSpacing.X * 2);
                        }

                        ImGui.Columns(1);

                        ImGui.EndChild();
                    }

                    ImGui.EndTabItem();
                }

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        private void SetUpReportModal(Message message) {
            ImGui.SetNextWindowSize(new Vector2(350, -1));
            if (!ImGui.BeginPopupModal($"Report to NoSoliciting###modal-message-{message.Id}")) {
                return;
            }

            ImGui.PushTextWrapPos();

            ImGui.Text("Reporting this message will let the developer know that you think this message was incorrectly classified.");

            if (message.FilterReason != null) {
                ImGui.Text("Specifically, this message WAS filtered but shouldn't have been.");
            } else {
                ImGui.Text("Specifically, this message WAS NOT filtered but should have been.");
            }

            ImGui.Separator();

            ImGui.Text(message.Content.TextValue);

            ImGui.Separator();

            if (message.FilterReason == "custom") {
                ImGui.TextColored(new Vector4(1f, 0f, 0f, 1f), "You cannot report messages filtered because of a custom filter.");
            } else {
                if (ImGui.Button("Report")) {
                    Task.Run(async () => {
                        string resp = null;
                        try {
                            using var client = new WebClient();
                            this.LastReportStatus = ReportStatus.InProgress;
                            resp = await client.UploadStringTaskAsync(this.Plugin.Definitions.ReportUrl, message.ToJson()).ConfigureAwait(true);
                        } catch (Exception) {
                            // ignored
                        }

                        this.LastReportStatus = resp == "{\"message\":\"ok\"}" ? ReportStatus.Successful : ReportStatus.Failure;
                        PluginLog.Log($"Report sent. Response: {resp}");
                    });
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
            }

            if (ImGui.Button("Cancel")) {
                ImGui.CloseCurrentPopup();
            }

            ImGui.PopTextWrapPos();

            ImGui.EndPopup();
        }

        private enum ReportStatus {
            None = -1,
            Failure = 0,
            Successful = 1,
            InProgress = 2,
        }

        private static bool AddColumn(IList<float> maxSizes, params string[] args) {
            var clicked = false;

            for (var i = 0; i < args.Length; i++) {
                var arg = args[i];
                var last = i == args.Length - 1;

                if (last) {
                    ImGui.PushTextWrapPos();
                }

                ImGui.TextUnformatted(arg);
                if (last) {
                    ImGui.PopTextWrapPos();
                }

                clicked = clicked || ImGui.IsItemClicked();
                if (!last) {
                    maxSizes[i] = Math.Max(maxSizes[i], ImGui.CalcTextSize(arg).X);
                }

                ImGui.NextColumn();
            }

            return clicked;
        }
    }
}
