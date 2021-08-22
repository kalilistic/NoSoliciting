using System;
using System.Linq;
using NoSoliciting.Resources;
using XivCommon.Functions.ContextMenu;

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

        private void OnOpenContextMenu(ContextMenuOpenArgs args) {
            if (args.ParentAddonName != "LookingForGroup") {
                return;
            }

            if (args.ContentIdLower == 0) {
                return;
            }

            var label = Language.ReportToNoSoliciting;
            args.Items.Add(new NormalContextMenuItem(label, this.Report));
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

            this.Plugin.Ui.Report.ToShowModal = message;
        }
    }
}
