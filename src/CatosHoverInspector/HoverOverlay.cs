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
        private static Hud _ownedHud;
        private static float _nextReadTime;

        internal static void Apply(Hud hud, Player player)
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
                _vanillaText = hud.m_hoverName.text;
                _nextReadTime = 0f;
            }

            float now = Time.unscaledTime;
            if (targetChanged || now >= _nextReadTime)
            {
                _nextReadTime = now + Math.Max(0.025f, ModConfig.UpdateIntervalMs.Value / 1000f);
                InspectionContext context = new InspectionContext(player, target, now);

                if (!InspectorRegistry.TryInspect(context, out InspectionResult result))
                {
                    ReleaseOwnedText(hud);
                    return;
                }

                if (!string.Equals(_fingerprint, result.Fingerprint, StringComparison.Ordinal))
                {
                    _fingerprint = result.Fingerprint;
                    string sourceText = _ownedHud == hud ? _vanillaText : hud.m_hoverName.text;
                    if (!HoverTextFormatter.TryFormat(sourceText, result, out _renderedText))
                    {
                        ReleaseOwnedText(hud);
                        return;
                    }
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
            _ownedHud = null;
            _nextReadTime = 0f;
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
        }
    }
}
