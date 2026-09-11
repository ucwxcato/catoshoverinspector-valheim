using UnityEngine;

namespace CatosHoverInspector
{
    // Transient main-thread context. Inspectors must copy display data into an
    // InspectionResult and must not retain these live references.
    internal sealed class InspectionContext
    {
        internal InspectionContext(Player player, GameObject hoverObject, float unscaledTime)
        {
            Player = player;
            HoverObject = hoverObject;
            TargetIdentity = HoverTargetController.GetTargetIdentity(hoverObject);
            UnscaledTime = unscaledTime;
        }

        internal Player Player { get; }
        internal GameObject HoverObject { get; }
        internal int TargetIdentity { get; }
        internal float UnscaledTime { get; }
    }
}
