using UnityEngine;

namespace CatosHoverInspector
{
    internal static class HoverTargetController
    {
        internal static bool TryGetNativeHoverTarget(Player player, out GameObject target)
        {
            target = null;
            if (!player || ModConfig.Enabled == null || !ModConfig.Enabled.Value)
                return false;

            // This is Valheim's own hover/range/lifecycle result. Do not add a
            // second camera raycast or retain the GameObject in a snapshot.
            target = player.GetHoverObject();
            return target;
        }

        internal static int GetTargetIdentity(GameObject target)
        {
            return target ? target.GetInstanceID() : 0;
        }
    }
}
