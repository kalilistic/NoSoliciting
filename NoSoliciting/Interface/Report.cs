using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using CheapLoc;
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

            var windowTitle = string.Format(Loc.Localize("Reporting", "{0} reporting"), this.Plugin.Name);
            if (!ImGui.Begin($"{windowTitle}###NoSoliciting reporting", ref this._showReporting)) {
                return;
            }

            ImGui.TextUnformatted(Loc.Localize("ReportHelp", "Click on one of the entries below to report it to the developer as miscategorised."));

            if (this.LastReportStatus != ReportStatus.None) {
                var status = this.LastReportStatus switch {
                    ReportStatus.Failure => Loc.Localize("ReportStatusFailure", "failed to send"),
                    ReportStatus.Successful => Loc.Localize("ReportStatusSuccessful", "sent successfully"),
                    ReportStatus.InProgress => Loc.Localize("ReportStatusInProgress", "sending"),
                    _ => Loc.Localize("ReportStatusUnknown", "unknown"),
                };
                var reportStatus = Loc.Localize("ReportStatusMessage", "Last report status: {0}");
                ImGui.TextUnformatted(string.Format(reportStatus, status));
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
            var tabTitle = Loc.Localize("ReportChatTab", "Chat");
            if (!ImGui.BeginTabItem($"{tabTitle}##chat-report")) {
                return;
            }

            if (ImGui.BeginChild("##chat-messages", new Vector2(-1, -1))) {
                if (ImGui.BeginTable("nosol-chat-report-table", 5, TableFlags)) {
                    ImGui.TableSetupColumn(Loc.Localize("ReportColumnTimestamp", "Timestamp"));
                    ImGui.TableSetupColumn(Loc.Localize("ReportColumnChannel", "Channel"));
                    ImGui.TableSetupColumn(Loc.Localize("ReportColumnReason", "Reason"));
                    ImGui.TableSetupColumn(Loc.Localize("ReportColumnSender", "Sender"));
                    ImGui.TableSetupColumn(Loc.Localize("ReportColumnMessage", "Message"), ImGuiTableColumnFlags.WidthStretch);
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
            var tabTitle = Loc.Localize("ReportPartyFinderTab", "Party Finder");
            if (!ImGui.BeginTabItem($"{tabTitle}##pf-report")) {
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
                    ImGui.TableSetupColumn(Loc.Localize("ReportColumnTimestamp", "Timestamp"));
                    ImGui.TableSetupColumn(Loc.Localize("ReportColumnReason", "Reason"));
                    ImGui.TableSetupColumn(Loc.Localize("ReportColumnHost", "Host"));
                    ImGui.TableSetupColumn(Loc.Localize("ReportColumnDescription", "Description"), ImGuiTableColumnFlags.WidthStretch);
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

            var modalTitle = string.Format(Loc.Localize("ReportModalTitle", "Report to {0}"), this.Plugin.Name);
            if (!ImGui.BeginPopupModal($"{modalTitle}###modal-message-{message.Id}")) {
                return;
            }

            ImGui.PushTextWrapPos();

            ImGui.TextUnformatted(Loc.Localize("ReportModalHelp1", "Reporting this message will let the developer know that you think this message was incorrectly classified."));

            ImGui.TextUnformatted(message.FilterReason != null
                ? Loc.Localize("ReportModalWasFiltered", "Specifically, this message WAS filtered but shouldn't have been.")
                : Loc.Localize("ReportModalWasNotFiltered", "Specifically, this message WAS NOT filtered but should have been."));

            ImGui.Separator();

            ImGui.TextUnformatted(message.Content.TextValue);

            ImGui.Separator();

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
            ImGui.TextUnformatted(Loc.Localize("ReportModalHelp2", "NoSoliciting only works for English messages. Do not report non-English messages."));
            ImGui.PopStyleColor();

            ImGui.Separator();

            if (message.FilterReason == "custom") {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                ImGui.TextUnformatted(Loc.Localize("ReportModalCustom", "You cannot report messages filtered because of a custom filter."));
                ImGui.PopStyleColor();
            } else {
                var buttonTitle = Loc.Localize("ReportModalReport", "Report");
                if (ImGui.Button($"{buttonTitle}##report-submit-{message.Id}")) {
                    this.ReportMessage(message);
                    ImGui.CloseCurrentPopup();
                }

                ImGui.SameLine();
            }

            var copyButton = Loc.Localize("ReportModalCopy", "Copy to clipboard");
            if (ImGui.Button($"{copyButton}##report-copy-{message.Id}")) {
                ImGui.SetClipboardText(message.Content.TextValue);
            }

            #if DEBUG
            ImGui.SameLine();
            if (ImGui.Button("Copy CSV")) {
                ImGui.SetClipboardText(message.ToCsv().ToString());
            }
            #endif

            ImGui.SameLine();

            var cancelButton = Loc.Localize("ReportModalCancel", "Cancel");
            if (ImGui.Button($"{cancelButton}##report-cancel-{message.Id}")) {
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

        internal void ReportMessage(Message message) {
            Task.Run(async () => await this.ReportMessageAsync(message));
        }

        internal async Task<ReportStatus> ReportMessageAsync(Message message) {
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

            var status = resp == "{\"message\":\"ok\"}" ? ReportStatus.Successful : ReportStatus.Failure;
            if (status == ReportStatus.Failure) {
                PluginLog.LogWarning($"Failed to report message:\n{resp}");
            }

            this.LastReportStatus = status;
            PluginLog.Log(resp == null
                ? "Report not sent. ML model not set."
                : $"Report sent. Response: {resp}");

            return status;
        }

        #endregion
    }
}
