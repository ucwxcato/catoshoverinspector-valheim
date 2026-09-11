using System.Collections.Generic;

namespace CatosHoverInspector
{
    // Disabled by default. This gives Phase 1 a harmless, deterministic line
    // for validating native target resolution before object-specific readers
    // are introduced in later phases.
    internal sealed class NativeHoverSmokeInspector : IHoverInspector
    {
        public string Id { get { return "native-hover-smoke"; } }
        public int Priority { get { return int.MinValue; } }

        public bool CanInspect(InspectionContext context)
        {
            return context != null && context.HoverObject;
        }

        public bool TryInspect(InspectionContext context, out InspectionResult result)
        {
            result = new InspectionResult(
                "native-hover-smoke",
                "Catos Hover Inspector",
                new List<DisplayLine>
                {
                    new DisplayLine("Pipeline", "Native hover target resolved")
                },
                new EtaDescriptor[0],
                new string[0]);
            return true;
        }
    }
}
