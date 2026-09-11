using HarmonyLib;
using System;

namespace CatosHoverInspector
{
    [HarmonyPatch(typeof(Hud), "UpdateCrosshair")]
    internal static class HoverInspectorPatches
    {
        private static void Postfix(Hud __instance, Player player, float bowDrawPercentage)
        {
            try
            {
                // This runs after Valheim has rebuilt m_hoverName. Capture
                // that native result before the overlay is applied so a
                // distance-only change ([E] Use <-> Too far) is observable.
                HoverOverlay.Apply(__instance, player, true);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Native hover update failed: {ex}");
                HoverOverlay.Clear(__instance);
            }
        }
    }
}
