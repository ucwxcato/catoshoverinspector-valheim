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

        internal static bool TryGetNativeHoverText(GameObject target, out string text)
        {
            text = null;
            if (!target)
                return false;

            Hoverable hoverable = target.GetComponentInParent<Hoverable>();
            if (hoverable == null)
                return false;

            // Ask the hovered object directly each frame. Hud.UpdateCrosshair
            // can leave our previously composed text in m_hoverName between
            // updates, while GetHoverText recalculates live range-dependent
            // actions such as [E] Use and Too far.
            text = hoverable.GetHoverText() ?? string.Empty;
            if (ZInput.IsGamepadActive())
            {
                text = text.Replace("[<color=yellow><b><sprite=", "<sprite=")
                    .Replace("></b></color>]", ">");
            }

            return true;
        }
    }
}
