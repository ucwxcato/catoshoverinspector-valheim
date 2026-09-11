using System;
using System.Collections.Generic;

namespace CatosHoverInspector
{
    internal interface IHoverInspector
    {
        string Id { get; }
        int Priority { get; }
        bool CanInspect(InspectionContext context);
        bool TryInspect(InspectionContext context, out InspectionResult result);
    }

    internal static class InspectorRegistry
    {
        private static readonly List<IHoverInspector> Inspectors = new List<IHoverInspector>();
        private static readonly Dictionary<string, float> NextErrorLog = new Dictionary<string, float>();

        internal static void Register(IHoverInspector inspector)
        {
            if (inspector == null || string.IsNullOrEmpty(inspector.Id))
                throw new ArgumentException("Inspector must have a stable ID.", nameof(inspector));

            Inspectors.Add(inspector);
            Inspectors.Sort((left, right) => right.Priority.CompareTo(left.Priority));
        }

        internal static bool TryInspect(InspectionContext context, out InspectionResult result)
        {
            result = null;
            for (int index = 0; index < Inspectors.Count; index++)
            {
                IHoverInspector inspector = Inspectors[index];
                try
                {
                    if (!inspector.CanInspect(context))
                        continue;

                    if (inspector.TryInspect(context, out result) && result != null)
                        return true;
                }
                catch (Exception ex)
                {
                    LogInspectorFailure(inspector, ex, context.UnscaledTime);
                }
            }

            return false;
        }

        private static void LogInspectorFailure(IHoverInspector inspector, Exception exception, float now)
        {
            float nextAllowed;
            if (NextErrorLog.TryGetValue(inspector.Id, out nextAllowed) && now < nextAllowed)
                return;

            NextErrorLog[inspector.Id] = now + 2f;
            Plugin.Log?.LogWarning($"Inspector '{inspector.Id}' failed; leaving native hover text unchanged: {exception.Message}");
        }
    }
}
