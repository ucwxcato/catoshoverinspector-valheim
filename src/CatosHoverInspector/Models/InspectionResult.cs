using System.Collections.Generic;

namespace CatosHoverInspector
{
    internal sealed class InspectionResult
    {
        internal InspectionResult(
            string fingerprint,
            string header,
            IReadOnlyList<DisplayLine> lines,
            IReadOnlyList<EtaDescriptor> etas,
            IReadOnlyList<string> warnings)
        {
            Fingerprint = fingerprint ?? string.Empty;
            Header = header ?? string.Empty;
            Lines = lines ?? new DisplayLine[0];
            Etas = etas ?? new EtaDescriptor[0];
            Warnings = warnings ?? new string[0];
        }

        internal string Fingerprint { get; }
        internal string Header { get; }
        internal IReadOnlyList<DisplayLine> Lines { get; }
        internal IReadOnlyList<EtaDescriptor> Etas { get; }
        internal IReadOnlyList<string> Warnings { get; }
    }
}
