using System;
using UnityEngine;

namespace CatosHoverInspector
{
    internal static class HoverOverlay
    {
        private static int _targetIdentity;
        private static string _fingerprint;
        private static string _renderedText;
        private static string _vanillaText;
        private static string _formattedVanillaText;
        private static Hud _ownedHud;
        private static int _lastProcessedFrame = -1;

        internal static void Apply(Hud hud, Player player, bool nativeRefresh = false)
        {
            if (!hud || !player || ModConfig.Enabled == null || !ModConfig.Enabled.Value ||
                hud.m_hoverName == null)
            {
                Clear(hud);
                return;
            }

            if (!HoverTargetController.TryGetNativeHoverTarget(player, out GameObject target))
            {
                Clear(hud);
                return;
            }

            int targetIdentity = HoverTargetController.GetTargetIdentity(target);
            bool targetChanged = targetIdentity != _targetIdentity;
            if (targetChanged)
            {
                ReleaseOwnedText(hud);
                _targetIdentity = targetIdentity;
                _fingerprint = null;
                _renderedText = null;
                _formattedVanillaText = null;
                _lastProcessedFrame = -1;
            }

            if (nativeRefresh)
                _vanillaText = hud.m_hoverName.text;
            else if (targetChanged || _vanillaText == null)
                _vanillaText = hud.m_hoverName.text;

            // Postfix and LateUpdate can both reach this method in one frame.
            // Inspect once per Unity frame, then re-apply the cached result in
            // LateUpdate if another HUD writer changed the visible text.
            if (targetChanged || _lastProcessedFrame != Time.frameCount)
            {
                _lastProcessedFrame = Time.frameCount;
                float now = Time.unscaledTime;
                InspectionContext context = new InspectionContext(player, target, now);

                if (!InspectorRegistry.TryInspect(context, out InspectionResult result))
                {
                    ReleaseOwnedText(hud);
                    return;
                }

                if (!string.Equals(_fingerprint, result.Fingerprint, StringComparison.Ordinal) ||
                    !string.Equals(_formattedVanillaText, _vanillaText, StringComparison.Ordinal))
                {
                    _fingerprint = result.Fingerprint;
                    string sourceText = _ownedHud == hud ? _vanillaText : hud.m_hoverName.text;
                    if (!HoverTextFormatter.TryFormat(sourceText, result, out _renderedText))
                    {
                        ReleaseOwnedText(hud);
                        return;
                    }

                    _formattedVanillaText = _vanillaText;
                }
            }

            if (!string.IsNullOrEmpty(_renderedText))
            {
                if (_ownedHud != hud)
                {
                    _vanillaText = hud.m_hoverName.text;
                    _ownedHud = hud;
                }

                hud.m_hoverName.text = _renderedText;
            }
        }

        internal static void Clear(Hud hud)
        {
            ReleaseOwnedText(hud);
            _targetIdentity = 0;
            _fingerprint = null;
            _renderedText = null;
            _vanillaText = null;
            _formattedVanillaText = null;
            _ownedHud = null;
            _lastProcessedFrame = -1;
        }

        private static void ReleaseOwnedText(Hud hud)
        {
            if (_ownedHud && _ownedHud.m_hoverName != null &&
                string.Equals(_ownedHud.m_hoverName.text, _renderedText, StringComparison.Ordinal))
            {
                _ownedHud.m_hoverName.text = _vanillaText ?? string.Empty;
            }
            else if (hud && hud.m_hoverName != null &&
                string.Equals(hud.m_hoverName.text, _renderedText, StringComparison.Ordinal))
            {
                hud.m_hoverName.text = _vanillaText ?? string.Empty;
            }

            _ownedHud = null;
            _renderedText = null;
            _vanillaText = null;
            _formattedVanillaText = null;
        }
    }
}
