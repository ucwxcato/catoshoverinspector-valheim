using System;
using System.Text;

namespace CatosHoverInspector
{
    internal static class HoverTextFormatter
    {
        internal static bool TryFormat(string vanillaText, InspectionResult result, out string text)
        {
            text = null;
            if (result == null)
                return false;

            int maxLines = Math.Max(1, ModConfig.MaxLines.Value);
            int maxCharacters = Math.Max(100, ModConfig.MaxTextCharacters.Value);
            StringBuilder builder = new StringBuilder();
            int lineCount = 0;

            if (result.PreserveNativeText && ModConfig.ShowVanillaName.Value &&
                !string.IsNullOrEmpty(vanillaText))
                AppendLine(builder, vanillaText, ref lineCount, maxLines);
            if (!string.IsNullOrEmpty(result.Header))
                AppendLine(builder, EscapeRichText(SafeText.CleanOrFallback(result.Header, "Inspector")),
                    ref lineCount, maxLines);

            for (int index = 0; index < result.Lines.Count && lineCount < maxLines; index++)
            {
                DisplayLine line = result.Lines[index];
                AppendLine(builder, FormatLine(line.Label, line.Value), ref lineCount, maxLines);
            }

            if (ModConfig.ShowWarnings.Value)
            {
                int warningCount = 0;
                for (int index = 0; index < result.Warnings.Count &&
                     lineCount < maxLines && warningCount < ModConfig.MaxWarnings.Value; index++)
                {
                    string warning = result.Warnings[index];
                    string warningText = EscapeRichText(SafeText.CleanOrFallback(warning, "Warning"));
                    if (ModConfig.UseRichTextColors.Value)
                        warningText = "<color=#FFB347>" + warningText + "</color>";
                    AppendLine(builder, warningText,
                        ref lineCount, maxLines);
                    warningCount++;
                }
            }

            for (int index = 0; index < result.Etas.Count && lineCount < maxLines; index++)
            {
                if (EtaFormatter.TryFormat(result.Etas[index], out string etaText))
                    AppendLine(builder, EscapeRichText(SafeText.CleanOrFallback(etaText, "ETA unavailable")),
                        ref lineCount, maxLines);
                else if (ModConfig.ShowUnavailableEta.Value && result.Etas[index].Label.Length > 0)
                    AppendLine(builder, EscapeRichText(SafeText.CleanOrFallback(
                        result.Etas[index].Label + ": Unavailable", "ETA unavailable")),
                        ref lineCount, maxLines);
            }

            if (lineCount == 0)
                return false;

            text = builder.ToString();
            if (text.Length > maxCharacters)
                text = text.Substring(0, Math.Max(1, maxCharacters - 1)) + "…";
            return true;
        }

        private static string FormatLine(string label, string value)
        {
            string safeLabel = EscapeRichText(SafeText.CleanOrFallback(label, "Unknown"));
            string safeValue = EscapeRichText(SafeText.Clean(value));
            return string.IsNullOrEmpty(safeValue) ? safeLabel : safeLabel + ": " + safeValue;
        }

        private static void AppendLine(StringBuilder builder, string line, ref int lineCount, int maxLines)
        {
            if (lineCount >= maxLines || string.IsNullOrEmpty(line))
                return;

            if (builder.Length > 0)
                builder.AppendLine();
            builder.Append(line);
            lineCount++;
        }

        internal static string EscapeRichText(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value.Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }
    }
}
