using System;
using System.Text.RegularExpressions;

namespace CatosHoverInspector
{
    internal static class SafeText
    {
        private static readonly Regex RichTextTag = new Regex("<[^>]*>", RegexOptions.Compiled);

        internal static string CleanOrFallback(string value, string fallback)
        {
            string cleaned = Clean(value);
            if (cleaned.Length == 0)
                return fallback;
            if (cleaned.StartsWith("$", StringComparison.Ordinal) ||
                cleaned.StartsWith("item_", StringComparison.OrdinalIgnoreCase) ||
                cleaned.StartsWith("piece_", StringComparison.OrdinalIgnoreCase))
                return HumanizeKey(cleaned);
            return cleaned;
        }

        internal static string Clean(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            return RichTextTag.Replace(value, string.Empty)
                .Replace('\r', ' ')
                .Replace('\n', ' ')
                .Trim();
        }

        internal static string HumanizeKey(string value)
        {
            string key = Clean(value);
            if (key.StartsWith("$", StringComparison.Ordinal))
                key = key.Substring(1);
            if (key.StartsWith("item_", StringComparison.OrdinalIgnoreCase))
                key = key.Substring(5);
            if (key.StartsWith("piece_", StringComparison.OrdinalIgnoreCase))
                key = key.Substring(6);
            key = key.Replace('_', ' ').Trim();
            if (key.Length == 0)
                return "Unknown";
            return char.ToUpperInvariant(key[0]) + key.Substring(1);
        }
    }
}
