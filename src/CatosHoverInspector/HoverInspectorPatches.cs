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
                HoverOverlay.Apply(__instance, player);
            }
            catch (Exception ex)
            {
                Plugin.Log?.LogError($"Native hover update failed: {ex}");
                HoverOverlay.Clear(__instance);
            }
        }
    }
}
