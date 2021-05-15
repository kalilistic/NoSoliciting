﻿using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Numerics;
using System.Threading.Tasks;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Plugin;
using ImGuiNET;
using NoSoliciting.Ml;
using NoSoliciting.Resources;

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

        internal Message? ToShowModal { get; set; }

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
            var toShow = this.ToShowModal;
            if (toShow != null) {
                if (!this.SetUpReportModal(toShow)) {
                    ImGui.OpenPopup($"###modal-message-{toShow.Id}");
                }
            }

            if (!this.ShowReporting) {
                return;
            }

            ImGui.SetNextWindowSize(new Vector2(1_000, 350), ImGuiCond.FirstUseEver);

            var windowTitle = string.Format(Language.Reporting, this.Plugin.Name);
            if (!ImGui.Begin($"{windowTitle}###NoSoliciting reporting", ref this._showReporting)) {
                return;
            }

            ImGui.TextUnformatted(Language.ReportHelp);

            if (this.LastReportStatus != ReportStatus.None) {
                var status = this.LastReportStatus switch {
                    ReportStatus.Failure => Language.ReportStatusFailure,
                    ReportStatus.Successful => Language.ReportStatusSuccessful,
                    ReportStatus.InProgress => Language.ReportStatusInProgress,
                    _ => Language.ReportStatusUnknown,
                };
                var reportStatus = Language.ReportStatusMessage;
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
            if (!ImGui.BeginTabItem($"{Language.ReportChatTab}###chat-report")) {
                return;
            }

            if (ImGui.BeginChild("##chat-messages", new Vector2(-1, -1))) {
                if (ImGui.BeginTable("nosol-chat-report-table", 5, TableFlags)) {
                    ImGui.TableSetupColumn(Language.ReportColumnTimestamp);
                    ImGui.TableSetupColumn(Language.ReportColumnChannel);
                    ImGui.TableSetupColumn(Language.ReportColumnReason);
                    ImGui.TableSetupColumn(Language.ReportColumnSender);
                    ImGui.TableSetupColumn(Language.ReportColumnMessage, ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    foreach (var message in this.Plugin.MessageHistory) {
                        ImGui.TableNextRow();

                        if (message.Filtered) {
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(238f / 255f, 71f / 255f, 71f / 255f, 1f));
                        }

                        var sender = message.Sender.Payloads
                            .Where(payload => payload.Type == PayloadType.RawText)
                            .Cast<TextPayload>()
                            .Select(payload => payload.Text)
                            .FirstOrDefault() ?? "";

                        if (AddRow(message.Timestamp.ToString(CultureInfo.CurrentCulture), message.ChatType.Name(this.Plugin.Interface.Data), message.FilterReason ?? string.Empty, sender, message.Content.TextValue)) {
                            ImGui.OpenPopup($"###modal-message-{message.Id}");
                        }

                        if (message.Filtered) {
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
            if (!ImGui.BeginTabItem($"{Language.ReportPartyFinderTab}###pf-report")) {
                return;
            }

            #if DEBUG
            if (ImGui.Button("Copy CSV")) {
                var builder = new StringBuilder();

                foreach (var message in this.Plugin.PartyFinderHistory) {
                    if (message.Classification == null) {
                        continue;
                    }

                    message.ToCsv(builder).Append('\n');
                }

                ImGui.SetClipboardText(builder.ToString());
            }
            #endif

            if (ImGui.BeginChild("##pf-messages", new Vector2(-1, -1))) {
                if (ImGui.BeginTable("nosol-pf-report-table", 4, TableFlags)) {
                    ImGui.TableSetupColumn(Language.ReportColumnTimestamp);
                    ImGui.TableSetupColumn(Language.ReportColumnReason);
                    ImGui.TableSetupColumn(Language.ReportColumnHost);
                    ImGui.TableSetupColumn(Language.ReportColumnDescription, ImGuiTableColumnFlags.WidthStretch);
                    ImGui.TableSetupScrollFreeze(0, 1);
                    ImGui.TableHeadersRow();

                    foreach (var message in this.Plugin.PartyFinderHistory) {
                        ImGui.TableNextRow();

                        if (message.Filtered) {
                            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(238f / 255f, 71f / 255f, 71f / 255f, 1f));
                        }

                        var sender = message.Sender.Payloads
                            .Where(payload => payload.Type == PayloadType.RawText)
                            .Cast<TextPayload>()
                            .Select(payload => payload.Text)
                            .FirstOrDefault() ?? "";

                        if (AddRow(message.Timestamp.ToString(CultureInfo.CurrentCulture), message.FilterReason ?? string.Empty, sender, message.Content.TextValue)) {
                            ImGui.OpenPopup($"###modal-message-{message.Id}");
                        }

                        if (message.Filtered) {
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

        private MessageCategory? _reportCategory;

        /// <summary>
        ///
        /// </summary>
        /// <param name="message"></param>
        /// <returns>true if modal is closing</returns>
        private bool SetUpReportModal(Message message) {
            var closing = false;

            ImGui.SetNextWindowSize(new Vector2(350, -1));

            var modalTitle = string.Format(Language.ReportModalTitle, this.Plugin.Name);
            if (!ImGui.BeginPopupModal($"{modalTitle}###modal-message-{message.Id}")) {
                return false;
            }

            if (this._reportCategory == null) {
                if (message.Classification != null) {
                    this._reportCategory = message.Classification;
                } else if (message.Classification == null && !message.Custom && !message.ItemLevel) {
                    this._reportCategory = MessageCategory.Normal;
                }
            }

            ImGui.PushTextWrapPos();

            ImGui.TextUnformatted(Language.ReportModalHelp1);

            ImGui.Separator();

            ImGui.TextUnformatted(message.Content.TextValue);

            ImGui.Separator();

            ImGui.TextUnformatted(string.Format(Language.ReportModalOriginalClassification, message.FilterReason ?? MessageCategory.Normal.Name()));

            ImGui.TextUnformatted(Language.ReportModalSuggestedClassification);

            ImGui.SetNextItemWidth(-1);
            if (ImGui.BeginCombo($"##modal-classification-{message.Id}", this._reportCategory?.Name() ?? string.Empty)) {
                foreach (var category in (MessageCategory[]) Enum.GetValues(typeof(MessageCategory))) {
                    if (ImGui.Selectable($"{category.Name()}##modal-option-{message.Id}", this._reportCategory == category)) {
                        this._reportCategory = category;
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

                ImGui.EndCombo();
            }

            ImGui.Separator();

            ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
            ImGui.TextUnformatted(Language.ReportModalHelp2);
            ImGui.PopStyleColor();

            ImGui.Separator();

            string? errorText = null;
            if (message.Custom) {
                errorText = Language.ReportModalDisabledCustom;
            } else if (message.ItemLevel) {
                errorText = Language.ReportModalDisabledItemLevel;
            } else if (message.ModelVersion == null) {
                errorText = Language.ReportModalDisabledBadModel;
            } else if (this._reportCategory == message.Classification) {
                errorText = Language.ReportModalDisabledSameClassification;
            } else {
                switch (this.Plugin.Config.AdvancedMode) {
                    case true when this.Plugin.Config.MlFilters.Values.All(set => set.Count == 0):
                    case false when this.Plugin.Config.BasicMlFilters.Count == 0:
                        errorText = Language.ReportModalDisabledNoFilters;
                        break;
                }
            }

            if (errorText != null) {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1f, 0f, 0f, 1f));
                ImGui.TextUnformatted(errorText);
                ImGui.PopStyleColor();
            } else {
                if (ImGui.Button($"{Language.ReportModalReport}##report-submit-{message.Id}")) {
                    var suggested = this._reportCategory?.ToModelName() ?? "none (this is a bug)";
                    this._reportCategory = null;
                    if (message == this.ToShowModal) {
                        Task.Run(async () => {
                            var status = await this.Plugin.Ui.Report.ReportMessageAsync(message, suggested);
                            switch (status) {
                                case ReportStatus.Successful: {
                                    var msg = Language.ReportToastSuccess;
                                    this.Plugin.Interface.Framework.Gui.Toast.ShowNormal(string.Format(msg, message.Sender));
                                    break;
                                }
                                case ReportStatus.Failure: {
                                    var msg = Language.ReportToastFailure;
                                    this.Plugin.Interface.Framework.Gui.Toast.ShowError(string.Format(msg, message.Sender));
                                    break;
                                }
                            }
                        });
                        this.ToShowModal = null;
                    } else {
                        this.ReportMessage(message, suggested);
                    }

                    ImGui.CloseCurrentPopup();
                    closing = true;
                }

                ImGui.SameLine();
            }

            if (ImGui.Button($"{Language.ReportModalCopy}##report-copy-{message.Id}")) {
                ImGui.SetClipboardText(message.Content.TextValue);
            }

            #if DEBUG
            ImGui.SameLine();
            if (ImGui.Button("Copy CSV")) {
                ImGui.SetClipboardText(message.ToCsv().ToString());
            }
            #endif

            ImGui.SameLine();

            var cancelButton = Language.ReportModalCancel;
            if (ImGui.Button($"{cancelButton}##report-cancel-{message.Id}")) {
                this._reportCategory = null;
                if (message == this.ToShowModal) {
                    this.ToShowModal = null;
                }

                ImGui.CloseCurrentPopup();
                closing = true;
            }

            ImGui.PopTextWrapPos();

            ImGui.EndPopup();

            return closing;
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

        private void ReportMessage(Message message, string suggested) {
            Task.Run(async () => await this.ReportMessageAsync(message, suggested));
        }

        private async Task<ReportStatus> ReportMessageAsync(Message message, string suggested) {
            string? resp = null;
            try {
                using var client = new WebClient();
                this.LastReportStatus = ReportStatus.InProgress;
                var reportUrl = this.Plugin.MlFilter?.ReportUrl;
                if (reportUrl != null) {
                    var json = message.ToJson(suggested);
                    if (json != null) {
                        resp = await client.UploadStringTaskAsync(reportUrl, json).ConfigureAwait(true);
                    }
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
