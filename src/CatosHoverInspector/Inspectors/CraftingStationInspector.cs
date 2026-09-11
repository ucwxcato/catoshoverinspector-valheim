using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace CatosHoverInspector
{
    internal sealed class CraftingStationInspector : IHoverInspector
    {
        public string Id { get { return "crafting-station"; } }
        public int Priority { get { return 100; } }

        public bool CanInspect(InspectionContext context)
        {
            CraftingStation station;
            return ModConfig.EnableCraftingStationInspector.Value &&
                TryResolve(context, out station);
        }

        public bool TryInspect(InspectionContext context, out InspectionResult result)
        {
            result = null;
            if (!TryResolve(context, out CraftingStation station))
                return false;

            int level = Math.Max(0, station.GetLevel(false));
            int extensionCount = Math.Max(0, station.GetExtentionCount(false));
            bool covered;
            string coverageText = TryReadCoverage(station, out covered)
                ? (covered ? "Yes" : "No")
                : "Unknown";
            bool usable = station.CheckUsable(context.Player, false);
            string stationName = SafeText.CleanOrFallback(station.GetHoverName(), "Crafting station");

            var lines = new List<DisplayLine>
            {
                new DisplayLine("Level", level.ToString(CultureInfo.InvariantCulture)),
                new DisplayLine("Extensions (detected)", extensionCount.ToString(CultureInfo.InvariantCulture)),
                new DisplayLine("Covered", coverageText),
                new DisplayLine("Usable", usable ? "Yes" : "No")
            };

            string fingerprint = string.Join("|", stationName,
                level.ToString(CultureInfo.InvariantCulture),
                extensionCount.ToString(CultureInfo.InvariantCulture),
                coverageText,
                usable ? "usable" : "not-usable");

            result = new InspectionResult(
                fingerprint,
                stationName,
                lines,
                new EtaDescriptor[0],
                new string[0],
                true);
            return true;
        }

        private static bool TryReadCoverage(CraftingStation station, out bool covered)
        {
            covered = false;
            if (!station.m_roofCheckPoint)
                return false;

            float cover;
            bool underRoof;
            Cover.GetCoverForPoint(station.m_roofCheckPoint.position, out cover, out underRoof, 0.5f);
            covered = underRoof && cover >= 0.7f;
            return true;
        }

        private static bool TryResolve(InspectionContext context, out CraftingStation station)
        {
            station = null;
            if (context == null || !context.HoverObject)
                return false;

            // Valheim's native hover object is often the collider child rather
            // than the station component itself. Keep the search bounded to
            // the target's parent/child hierarchy.
            station = context.HoverObject.GetComponentInParent<CraftingStation>() ??
                context.HoverObject.GetComponentInChildren<CraftingStation>(true);
            return station;
        }
    }
}
