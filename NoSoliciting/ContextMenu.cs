using System;
using System.Linq;
using Dalamud.ContextMenu;
using NoSoliciting.Resources;

namespace NoSoliciting {
    public class ContextMenu : IDisposable {
        private Plugin Plugin { get; }

        internal ContextMenu(Plugin plugin) {
            this.Plugin = plugin;

            this.Plugin.DalamudContextMenu.OnOpenGameObjectContextMenu += this.OnOpenContextMenu;
        }

        public void Dispose() {
            this.Plugin.DalamudContextMenu.OnOpenGameObjectContextMenu -= this.OnOpenContextMenu;
        }

        private void OnOpenContextMenu(GameObjectContextMenuOpenArgs args) {
            if (args.ParentAddonName != "LookingForGroup") {
                return;
            }

            if (args.ContentIdLower == 0) {
                return;
            }

            var label = Language.ReportToNoSoliciting;
            args.AddCustomItem(new GameObjectContextMenuItem(label, this.Report));
        }

        private void Report(GameObjectContextMenuItemSelectedArgs args) {
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
