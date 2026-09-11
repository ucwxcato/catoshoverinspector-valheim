using System;

namespace CatosHoverInspector
{
    internal static class EtaFormatter
    {
        internal static bool TryFormat(EtaDescriptor descriptor, out string text)
        {
            text = null;
            if (!descriptor.IsAvailable)
                return false;

            if (descriptor.State == EtaState.Paused)
            {
                text = descriptor.Label + ": Paused";
                return true;
            }

            if (descriptor.State == EtaState.Ready || descriptor.RemainingSeconds <= 0f)
            {
                text = descriptor.Label + ": Ready";
                return true;
            }

            if (descriptor.State != EtaState.Active || float.IsNaN(descriptor.RemainingSeconds) ||
                float.IsInfinity(descriptor.RemainingSeconds))
            {
                return false;
            }

            // Ceiling keeps a visible one-second buffer and never reports a
            // false zero before the native job is ready.
            long seconds = Math.Max(1L, (long)Math.Ceiling(descriptor.RemainingSeconds));
            text = descriptor.Label + ": " + FormatDuration(seconds);
            return true;
        }

        internal static string FormatDuration(long totalSeconds)
        {
            if (totalSeconds <= 0L)
                return "Ready";

            long days = totalSeconds / 86400L;
            totalSeconds %= 86400L;
            long hours = totalSeconds / 3600L;
            totalSeconds %= 3600L;
            long minutes = totalSeconds / 60L;
            long seconds = totalSeconds % 60L;

            if (days > 0L)
                return string.Format("{0}d {1:00}h", days, hours);
            if (hours > 0L)
                return string.Format("{0}h {1:00}m", hours, minutes);
            if (minutes > 0L)
                return string.Format("{0}m {1:00}s", minutes, seconds);
            return string.Format("{0}s", seconds);
        }
    }
}
