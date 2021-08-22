using Dalamud.Data;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NoSoliciting.Ml;

namespace NoSoliciting {
    public static class FilterUtil {
        private static int MaxItemLevel { get; set; }

        private enum Slot {
            MainHand,
            OffHand,
            Head,
            Chest,
            Hands,
            Waist,
            Legs,
            Feet,
            Earrings,
            Neck,
            Wrist,
            RingL,
            RingR,
        }

        private static Slot? SlotFromItem(Item item) {
            var cat = item.EquipSlotCategory.Value;
            if (cat == null) {
                return null;
            }

            if (cat.MainHand != 0) {
                return Slot.MainHand;
            }

            if (cat.Head != 0) {
                return Slot.Head;
            }

            if (cat.Body != 0) {
                return Slot.Chest;
            }

            if (cat.Gloves != 0) {
                return Slot.Hands;
            }

            if (cat.Waist != 0) {
                return Slot.Waist;
            }

            if (cat.Legs != 0) {
                return Slot.Legs;
            }

            if (cat.Feet != 0) {
                return Slot.Feet;
            }

            if (cat.OffHand != 0) {
                return Slot.OffHand;
            }

            if (cat.Ears != 0) {
                return Slot.Earrings;
            }

            if (cat.Neck != 0) {
                return Slot.Neck;
            }

            if (cat.Wrists != 0) {
                return Slot.Wrist;
            }

            if (cat.FingerL != 0) {
                return Slot.RingL;
            }

            if (cat.FingerR != 0) {
                return Slot.RingR;
            }

            return null;
        }

        public static int MaxItemLevelAttainable(DataManager data) {
            if (MaxItemLevel > 0) {
                return MaxItemLevel;
            }

            if (data == null) {
                throw new ArgumentNullException(nameof(data), "DataManager cannot be null");
            }

            var ilvls = new Dictionary<Slot, int>();

            foreach (var item in data.GetExcelSheet<Item>()!) {
                var slot = SlotFromItem(item);
                if (slot == null) {
                    continue;
                }

                var itemLevel = 0;
                var ilvl = item.LevelItem.Value;
                if (ilvl != null) {
                    itemLevel = (int) ilvl.RowId;
                }

                if (ilvls.TryGetValue((Slot) slot, out var currentMax) && currentMax > itemLevel) {
                    continue;
                }

                ilvls[(Slot) slot] = itemLevel;
            }

            MaxItemLevel = (int) ilvls.Values.Average();

            return MaxItemLevel;
        }
    }

    public static class Extensions {
        public static bool ContainsIgnoreCase(this string haystack, string needle) {
            return CultureInfo.InvariantCulture.CompareInfo.IndexOf(haystack, needle, CompareOptions.IgnoreCase) >= 0;
        }

        public static bool WasEnabled(this IEnumerable<MessageCategory> enabled, MessageCategory category) {
            return category == MessageCategory.Normal || enabled.Contains(category);
        }
    }
}
