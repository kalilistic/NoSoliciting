using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using ImGuiNET;

namespace NoSoliciting.Interface {
    public class Report {
        private const ImGuiTableFlags TableFlags = ImGuiTableFlags.Borders
                                                   & ~ImGuiTableFlags.BordersOuterV
                                                   | ImGuiTableFlags.PadOuterX
                                                   | ImGuiTableFlags.RowBg
                                                   | ImGuiTableFlags.SizingFixedFit
                                                   | ImGuiTableFlags.ScrollY
                                                   | ImGuiTableFlags.Hideable
                                                   | ImGuiTableFlags.Reorderable
                                                   | ImGuiTableFlags.Resizable;

        private Plugin Plugin { get; }

        private ReportStatus LastReportStatus { get; set; } = ReportStatus.None;

        private bool _showReporting;

        private bool ShowReporting {
            get => this._showReporting;
            set => this._showReporting = value;
        }

        public Report(Plugin plugin) {
            this.Plugin = plugin;
        }

        public void Open() {
            this.ShowReporting = true;
        }

        public void Toggle() {
            this.ShowReporting = !this.ShowReporting;
        }

        public void Draw() {
            if (!this.ShowReporting) {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(1_000, 350), ImGuiCond.FirstUseEver);

            if (!ImGui.Begin("NoSoliciting reporting", ref this._showReporting)) {
                return;
            }

            ImGui.TextUnformatted("Click on one of the entries below to report it to the developer as miscategorised.");

            if (this.LastReportStatus != ReportStatus.None) {
                var status = this.LastReportStatus switch {
                    ReportStatus.Failure => "failed to send",
                    ReportStatus.Successful => "sent successfully",
                    ReportStatus.InProgress => "sending",
                    _ => "unknown",
                };
                ImGui.TextUnformatted($"Last report status: {status}");
            }

            ImGui.Separator();
            ImGui.Spacing();

            if (ImGui.BeginTabBar("##report-tabs")) {
                this.ChatTab();
                this.PartyFinderTab();

                ImGui.EndTabBar();
            }

            ImGui.End();
        }

        private void ChatTab() {
            if (!ImGui.BeginTabItem("Chat##chat-report")) {
                return;
            }

            if (ImGui.BeginChild("##chat-messages", new Vector2(-1, -1))) {
                if (ImGui.BeginTable("nosol-chat-report-table", 5, TableFlags)) {
                    ImGui.TableSetupColumn("Timestamp");
                    ImGui.TableSetupColumn("Channel");
                    ImGui.TableSetupColumn("Reason");
                    ImGui.TableSetupColumn("Sender");
                    ImGui.TableSetupColumn("Message", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    foreach (var message in this.Plugin.MessageHistory) {
                        ImGui.TableNextRow();

                        if (message.FilterReason != null) {
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(238f / 255f, 71f / 255f, 71f / 255f, 1f));
                        }

                        var sender = message.Sender.Payloads
                            .Where(payload => payload.Type == PayloadType.RawText)
                            .Cast<TextPayload>()
                            .Select(payload => payload.Text)
                            .FirstOrDefault() ?? "";

                        if (AddRow(message.Timestamp.ToString(CultureInfo.CurrentCulture), message.ChatType.Name(this.Plugin.Interface.Data), message.FilterReason ?? "", sender, message.Content.TextValue)) {
                            ImGui.OpenPopup($"###modal-message-{message.Id}");
                        }

                        if (message.FilterReason != null) {
                            ImGui.PopStyleColor();
                        }

                        this.SetUpReportModal(message);
                    }

                    ImGui.EndTable();
                }

                ImGui.EndChild();
            }

            ImGui.EndTabItem();
        }

        private void PartyFinderTab() {
            if (!ImGui.BeginTabItem("Party Finder##pf-report")) {
                return;
            }

            #if DEBUG
            if (ImGui.Button("Copy CSV")) {
                var builder = new StringBuilder();

                foreach (var message in this.Plugin.PartyFinderHistory) {
                    if (message.FilterReason == null) {
                        continue;
                    }

                    message.ToCsv(builder).Append('\n');
                }

                ImGui.SetClipboardText(builder.ToString());
            }
            #endif

            if (ImGui.BeginChild("##pf-messages", new Vector2(-1, -1))) {
                if (ImGui.BeginTable("nosol-pf-report-table", 4, TableFlags)) {
                    ImGui.TableSetupColumn("Timestamp");
                    ImGui.TableSetupColumn("Reason");
                    ImGui.TableSetupColumn("Host");
                    ImGui.TableSetupColumn("Description", ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    foreach (var message in this.Plugin.PartyFinderHistory) {
                        ImGui.TableNextRow();

                        if (message.FilterReason != null) {
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(238f / 255f, 71f / 255f, 71f / 255f, 1f));
                        }

                        var sender = message.Sender.Payloads
                            .Where(payload => payload.Type == PayloadType.RawText)
                            .Cast<TextPayload>()
                            .Select(payload => payload.Text)
                            .FirstOrDefault() ?? "";

                        if (AddRow(message.Timestamp.ToString(CultureInfo.CurrentCulture), message.FilterReason ?? "", sender, message.Content.TextValue)) {
                            ImGui.OpenPopup($"###modal-message-{message.Id}");
                        }

                        if (message.FilterReason != null) {
                            ImGui.PopStyleColor();
                        }

                        this.SetUpReportModal(message);
                    }

                    ImGui.EndTable();
                }

                ImGui.EndChild();
            }

            ImGui.EndTabItem();
        }

        #region Modal

        private void SetUpReportModal(Message message) {
            ImGui.SetNextWindowSize(new Vector2(350, -1));
            if (!ImGui.BeginPopupModal($"Report to NoSoliciting###modal-message-{message.Id}")) {
                return;
            }

            ImGui.PushTextWrapPos();

            if (!message.Ml) {
                ImGui.TextUnformatted("You cannot report messages filtered by definitions. Please switch to machine learning mode.");

                goto EndPopup;
            }

            ImGui.TextUnformatted("Reporting this message will let the developer know that you think this message was incorrectly classified.");

            ImGui.TextUnformatted(message.FilterReason != null
                ? "Specifically, this message WAS filtered but shouldn't have been."
                : "Specifically, this message WAS NOT filtered but should have been.");

            ImGui.Separator();

            ImGui.TextUnformatted(message.Content.TextValue);

            ImGui.Separator();

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
            ImGui.TextUnformatted("NoSoliciting only works for English messages. Do not report non-English messages.");
            ImGui.PopStyleColor();

            ImGui.Separator();

            if (message.FilterReason == "custom") {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                ImGui.TextUnformatted("You cannot report messages filtered because of a custom filter.");
                ImGui.PopStyleColor();
            } else {
                if (ImGui.Button("Report")) {
                    Task.Run(async () => {
                        string? resp = null;
                        try {
                            using var client = new WebClient();
                            this.LastReportStatus = ReportStatus.InProgress;
                            var reportUrl = this.Plugin.MlFilter?.ReportUrl;
                            if (reportUrl != null) {
                                resp = await client.UploadStringTaskAsync(reportUrl, message.ToJson()).ConfigureAwait(true);
                            }
                        } catch (Exception) {
                            // ignored
                        }

                        this.LastReportStatus = resp == "{\"message\":\"ok\"}" ? ReportStatus.Successful : ReportStatus.Failure;
                        PluginLog.Log(resp == null
                            ? "Report not sent. ML model not set."
                            : $"Report sent. Response: {resp}");
                    });
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
            }

            if (ImGui.Button("Copy to clipboard")) {
                ImGui.SetClipboardText(message.Content.TextValue);
            }

            #if DEBUG
            ImGui.SameLine();
            if (ImGui.Button("Copy CSV")) {
                ImGui.SetClipboardText(message.ToCsv().ToString());
            }
            #endif

            ImGui.SameLine();

            EndPopup:
            if (ImGui.Button("Cancel")) {
                ImGui.CloseCurrentPopup();
            }

            ImGui.PopTextWrapPos();

            ImGui.EndPopup();
        }

        #endregion

        #region Utility

        private static bool AddRow(params string[] args) {
            var clicked = false;

            for (var i = 0; i < args.Length; i++) {
                ImGui.TableNextColumn();

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
            }

            return clicked;
        }

        #endregion
    }
}
