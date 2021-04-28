using System;
using System.Linq;
using System.Threading.Tasks;
using NoSoliciting.Interface;
using NoSoliciting.Resources;
using XivCommon.Functions;

namespace NoSoliciting {
    public class ContextMenu : IDisposable {
        private Plugin Plugin { get; }

        internal ContextMenu(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.Common.Functions.ContextMenu.OpenContextMenu += this.OnOpenContextMenu;
        }

        public void Dispose() {
            this.Plugin.Common.Functions.ContextMenu.OpenContextMenu -= this.OnOpenContextMenu;
        }

        private void OnOpenContextMenu(ContextMenuArgs args) {
            if (args.ParentAddonName != "LookingForGroup") {
                return;
            }

            if (args.ContentIdLower == 0) {
                return;
            }

            var label = Language.ReportToNoSoliciting;
            args.AdditionalItems.Add(new ContextMenuItem(label, this.Report));
        }

        private void Report(ContextMenuItemSelectedArgs args) {
            if (args.ContentIdLower == 0) {
                return;
            }

            var listing = this.Plugin.Common.Functions.PartyFinder.CurrentListings
                .Values
                .FirstOrDefault(listing => listing.ContentIdLower == args.ContentIdLower);

            if (listing == null) {
                return;
            }

            var message = this.Plugin.PartyFinderHistory.FirstOrDefault(message => message.ActorId == listing.ContentIdLower);
            if (message == null) {
                return;
            }

            Task.Run(async () => {
                var status = await this.Plugin.Ui.Report.ReportMessageAsync(message);
                switch (status) {
                    case ReportStatus.Successful: {
                        var msg = Language.ReportToastSuccess;
                        this.Plugin.Interface.Framework.Gui.Toast.ShowNormal(string.Format(msg, listing.Name));
                        break;
                    }
                    case ReportStatus.Failure: {
                        var msg = Language.ReportToastFailure;
                        this.Plugin.Interface.Framework.Gui.Toast.ShowError(string.Format(msg, listing.Name));
                        break;
                    }
                }
            });
        }
    }
}
