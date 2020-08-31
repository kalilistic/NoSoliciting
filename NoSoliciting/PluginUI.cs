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
    public class PluginUI {
        private readonly Plugin plugin;
        private bool resizeWindow = false;
        private ReportStatus lastReportStatus = ReportStatus.None;

        private bool _showSettings;
        public bool ShowSettings { get => this._showSettings; set => this._showSettings = value; }

        private bool _showReporting;
        public bool ShowReporting { get => this._showReporting; set => this._showReporting = value; }

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

            if (this.ShowReporting) {
                this.DrawReportWindow();
            }
        }

        public void DrawSettings() {
            if (this.resizeWindow) {
                this.resizeWindow = false;
                ImGui.SetNextWindowSize(new Vector2(this.plugin.Config.AdvancedMode ? 650 : 0, 0));
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

                ImGui.SameLine();

                if (ImGui.Button("Show reporting window")) {
                    this.ShowReporting = true;
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

            bool filterHugeItemLevelPFs = this.plugin.Config.FilterHugeItemLevelPFs;
            if (ImGui.Checkbox("Filter PFs with item level above maximum", ref filterHugeItemLevelPFs)) {
                this.plugin.Config.FilterHugeItemLevelPFs = filterHugeItemLevelPFs;
                this.plugin.Config.Save();
            }
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

                        bool filterHugeItemLevelPFs = this.plugin.Config.FilterHugeItemLevelPFs;
                        if (ImGui.Checkbox("Enable built-in maximum item level filter", ref filterHugeItemLevelPFs)) {
                            this.plugin.Config.FilterHugeItemLevelPFs = filterHugeItemLevelPFs;
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
                this.plugin.Config.CompileRegexes();
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

        private void DrawReportWindow() {
            ImGui.SetNextWindowSize(new Vector2(1_000, 350), ImGuiCond.FirstUseEver);

            if (!ImGui.Begin("NoSoliciting reporting", ref this._showReporting)) {
                return;
            }

            ImGui.Text("Click on one of the entries below to report it to the developer as miscategorised.");

            if (this.lastReportStatus != ReportStatus.None) {
                string status;
                switch (this.lastReportStatus) {
                    case ReportStatus.Failure:
                        status = "failed to send";
                        break;
                    case ReportStatus.Successful:
                        status = "sent successfully";
                        break;
                    case ReportStatus.InProgress:
                        status = "sending";
                        break;
                    default:
                        status = "unknown";
                        break;
                }
                ImGui.Text($"Last report status: {status}");
            }

            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.BeginTabBar("##report-tabs")) {
                if (ImGui.BeginTabItem("Chat##chat-report")) {
                    float[] maxSizes = { 0f, 0f, 0f, 0f };

                    if (ImGui.BeginChild("##chat-messages", new Vector2(-1, -1))) {
                        ImGui.Columns(5);

                        AddColumn(maxSizes, "Timestamp", "Channel", "Reason", "Sender", "Message");
                        ImGui.Separator();

                        foreach (Message message in this.plugin.MessageHistory) {
                            if (message.FilterReason != null) {
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(238f / 255f, 71f / 255f, 71f / 255f, 1f));
                            }

                            string sender = message.Sender.Payloads
                                .Where(payload => payload.Type == PayloadType.RawText)
                                .Select(payload => (payload as TextPayload).Text)
                                .FirstOrDefault() ?? "";

                            if (AddColumn(maxSizes, message.Timestamp.ToString(CultureInfo.CurrentCulture), message.ChatType.ToString(), message.FilterReason ?? "", sender, message.Content.TextValue)) {
                                ImGui.OpenPopup($"###modal-message-{message.Id}");
                            }

                            if (message.FilterReason != null) {
                                ImGui.PopStyleColor();
                            }

                            this.SetUpReportModal(message);
                        }
                        for (int idx = 0; idx < maxSizes.Length; idx++) {
                            ImGui.SetColumnWidth(idx, maxSizes[idx] + ImGui.GetStyle().ItemSpacing.X * 2);
                        }
                        ImGui.Columns(1);

                        ImGui.EndChild();
                    }

                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem("Party Finder##pf-report")) {
                    float[] maxSizes = { 0f, 0f, 0f };

                    if (ImGui.BeginChild("##pf-messages", new Vector2(-1, -1))) {
                        ImGui.Columns(4);

                        AddColumn(maxSizes, "Timestamp", "Reason", "Host", "Description");
                        ImGui.Separator();

                        foreach (Message message in this.plugin.PartyFinderHistory) {
                            if (message.FilterReason != null) {
                                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(238f / 255f, 71f / 255f, 71f / 255f, 1f));
                            }

                            string sender = message.Sender.Payloads
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
                        for (int idx = 0; idx < maxSizes.Length; idx++) {
                            ImGui.SetColumnWidth(idx, maxSizes[idx] + ImGui.GetStyle().ItemSpacing.X * 2);
                        }
                        ImGui.Columns(1);

                        ImGui.EndChild();
                    }
                    ImGui.EndTabItem();
                }
            }

            ImGui.End();
        }

        private void SetUpReportModal(Message message) {
            ImGui.SetNextWindowSize(new Vector2(350, -1));
            if (ImGui.BeginPopupModal($"Report to NoSoliciting###modal-message-{message.Id}")) {
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
                }

                if (message.FilterReason != "custom" && ImGui.Button("Report")) {
                    Task.Run(async () => {
                        string resp = null;
                        try {
                            using (WebClient client = new WebClient()) {
                                this.lastReportStatus = ReportStatus.InProgress;
                                resp = await client.UploadStringTaskAsync(this.plugin.Definitions.ReportUrl, message.ToJson()).ConfigureAwait(true);
                            }
#pragma warning disable CA1031 // Do not catch general exception types
                        } catch (Exception) {}
#pragma warning restore CA1031 // Do not catch general exception types
                        this.lastReportStatus = resp == "{\"message\":\"ok\"}" ? ReportStatus.Successful : ReportStatus.Failure;
                        PluginLog.Log($"Report sent. Response: {resp}");
                    });
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();

                if (ImGui.Button("Cancel")) {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.PopTextWrapPos();

                ImGui.EndPopup();
            }
        }

        private enum ReportStatus {
            None = -1,
            Failure = 0,
            Successful = 1,
            InProgress = 2,
        }

        private static bool AddColumn(float[] maxSizes, params string[] args) {
            bool clicked = false;

            for (int i = 0; i < args.Length; i++) {
                string arg = args[i];
                bool last = i == args.Length - 1;

                if (last) {
                    ImGui.PushTextWrapPos();
                }
                ImGui.Text(arg);
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
