using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace CatosHoverInspector
{
    internal sealed class ProcessingInspector : IHoverInspector
    {
        public string Id { get { return "processing"; } }
        public int Priority { get { return 200; } }

        public bool CanInspect(InspectionContext context)
        {
            return ModConfig.EnableProcessingInspector.Value && context != null &&
                context.HoverObject && (ResolveWindmill(context.HoverObject) != null ||
                ResolveSmelter(context.HoverObject) != null ||
                ResolveFermenter(context.HoverObject) != null);
        }

        public bool TryInspect(InspectionContext context, out InspectionResult result)
        {
            result = null;
            if (context == null || !ModConfig.EnableProcessingInspector.Value)
                return false;

            Windmill windmill = ResolveWindmill(context.HoverObject);
            if (windmill != null)
                return TryInspectWindmill(windmill, out result);

            Smelter smelter = ResolveSmelter(context.HoverObject);
            if (smelter != null)
                return TryInspectSmelter(smelter, out result);

            Fermenter fermenter = ResolveFermenter(context.HoverObject);
            return fermenter != null && TryInspectFermenter(fermenter, out result);
        }

        private static bool TryInspectSmelter(Smelter smelter, out InspectionResult result)
        {
            result = null;
            SmelterReadings readings;
            if (!NativeTimeReader.TryReadSmelter(smelter, out readings))
                return false;

            bool hasInput = readings.InputCount > 0 && !string.IsNullOrEmpty(readings.InputName);
            bool hasFuel = readings.Fuel > 0f || readings.FuelCapacity <= 0;
            bool hasOutput = readings.OutputCount > 0;
            bool active = readings.Active && hasInput && hasFuel && readings.SecondsPerProduct > 0f &&
                readings.Power > 0.01f;
            float nextSeconds;
            bool hasNextEta = NativeTimeReader.TryGetSmelterNextEta(readings, active, out nextSeconds);

            string name = SafeText.CleanOrFallback(smelter.m_name, "Smelter");
            string status = GetSmelterStatus(readings, active, hasInput, hasFuel, hasOutput);
            var lines = new List<DisplayLine>
            {
                new DisplayLine("Status", status)
            };

            if (ModConfig.ShowInput.Value)
                lines.Add(new DisplayLine("Input", hasInput
                    ? FormatItem(readings.InputName) + " " + readings.InputCount.ToString(CultureInfo.InvariantCulture) +
                      FormatCapacity(readings.InputCapacity)
                    : "Empty"));
            if (ModConfig.ShowFuel.Value && readings.FuelCapacity > 0)
                lines.Add(new DisplayLine("Fuel", FormatNumber(readings.Fuel) + "/" +
                    readings.FuelCapacity.ToString(CultureInfo.InvariantCulture)));
            if (ModConfig.ShowOutput.Value)
                lines.Add(new DisplayLine("Output", hasOutput
                    ? readings.OutputCount.ToString(CultureInfo.InvariantCulture) +
                      (readings.OutputCapacity > 0
                          ? "/" + readings.OutputCapacity.ToString(CultureInfo.InvariantCulture) + " ready"
                          : " ready")
                    : "Empty"));
            if (ModConfig.ShowCapacities.Value)
                lines.Add(new DisplayLine("Capacity", "Input " + readings.InputCapacity.ToString(CultureInfo.InvariantCulture) +
                    ", Fuel " + readings.FuelCapacity.ToString(CultureInfo.InvariantCulture)));

            var etas = new List<EtaDescriptor>();
            if (hasNextEta)
            {
                etas.Add(new EtaDescriptor("Next output", EtaState.Active, nextSeconds));
                if (ModConfig.ShowBatchEta.Value && readings.InputCount > 1)
                {
                    float batchSeconds = nextSeconds + (readings.InputCount - 1) *
                        readings.SecondsPerProduct / Math.Max(0.01f, readings.Power);
                    etas.Add(new EtaDescriptor("Batch complete", EtaState.Active, batchSeconds));
                }
            }
            else if (hasInput && !active)
            {
                etas.Add(new EtaDescriptor("Next output", EtaState.Paused, 0f));
            }

            string fingerprint = string.Join("|", name, status, readings.InputName,
                readings.InputCount.ToString(CultureInfo.InvariantCulture),
                readings.Fuel.ToString("0.0", CultureInfo.InvariantCulture),
                readings.OutputCount.ToString(CultureInfo.InvariantCulture),
                hasNextEta ? NativeTimeReader.VisibleSeconds(nextSeconds).ToString(CultureInfo.InvariantCulture) : "paused");

            result = new InspectionResult(fingerprint, name, lines, etas, new string[0]);
            return true;
        }

        private static bool TryInspectFermenter(Fermenter fermenter, out InspectionResult result)
        {
            result = null;
            FermenterReadings readings;
            if (!NativeTimeReader.TryReadFermenter(fermenter, out readings))
                return false;

            string name = SafeText.CleanOrFallback(fermenter.GetHoverName(), "Fermenter");
            string status;
            EtaDescriptor eta = new EtaDescriptor("Ready in", EtaState.Unavailable, 0f);
            switch (readings.Status)
            {
                case 1:
                    status = "Fermenting";
                    float remaining = readings.Duration - (float)readings.ElapsedSeconds;
                    eta = remaining > 0f
                        ? new EtaDescriptor("Ready in", EtaState.Active, remaining)
                        : new EtaDescriptor("Ready in", EtaState.Ready, 0f);
                    break;
                case 2:
                    status = "Paused (exposed)";
                    eta = new EtaDescriptor("Ready in", EtaState.Paused, 0f);
                    break;
                case 3:
                    status = "Ready";
                    eta = new EtaDescriptor("Ready in", EtaState.Ready, 0f);
                    break;
                default:
                    status = "Empty";
                    break;
            }

            var lines = new List<DisplayLine>
            {
                new DisplayLine("Status", status)
            };
            if (readings.Content != 0)
                lines.Insert(0, new DisplayLine("Recipe", FormatItem(readings.ContentName)));

            var etas = new List<EtaDescriptor>();
            if (eta.IsAvailable || ModConfig.ShowUnavailableEta.Value)
                etas.Add(eta);

            string fingerprint = string.Join("|", name, status, readings.ContentName,
                eta.State.ToString(), NativeTimeReader.VisibleSeconds(eta.RemainingSeconds).ToString(CultureInfo.InvariantCulture));
            result = new InspectionResult(fingerprint, name, lines, etas, new string[0]);
            return true;
        }

        private static bool TryInspectWindmill(Windmill windmill, out InspectionResult result)
        {
            result = null;
            float power = Mathf.Clamp01(windmill.GetPowerOutput());
            string name = "Windmill";
            Smelter linkedSmelter;
            SmelterReadings linkedReadings = new SmelterReadings();
            bool hasLinkedSmelter = NativeTimeReader.TryGetLinkedSmelter(windmill, out linkedSmelter) &&
                NativeTimeReader.TryReadSmelter(linkedSmelter, out linkedReadings);
            bool linkedInput = hasLinkedSmelter && linkedReadings.InputCount > 0;
            bool linkedActive = linkedInput && linkedReadings.Active && power > 0.01f &&
                linkedReadings.SecondsPerProduct > 0f;
            string status = linkedActive ? "Running" :
                (power <= 0.01f ? "Paused (no wind)" : linkedInput ? "Paused" : "Idle");
            var lines = new List<DisplayLine>
            {
                new DisplayLine("Status", status),
                new DisplayLine("Power", (power * 100f).ToString("0", CultureInfo.InvariantCulture) + "%")
            };

            var etas = new List<EtaDescriptor>();
            if (hasLinkedSmelter)
            {
                if (ModConfig.ShowInput.Value)
                    lines.Add(new DisplayLine("Input", linkedInput
                        ? FormatItem(linkedReadings.InputName) + " " + linkedReadings.InputCount.ToString(CultureInfo.InvariantCulture) +
                          FormatCapacity(linkedReadings.InputCapacity)
                        : "Empty"));
                if (ModConfig.ShowOutput.Value)
                    lines.Add(new DisplayLine("Output", linkedReadings.OutputCount > 0
                        ? linkedReadings.OutputCount.ToString(CultureInfo.InvariantCulture) +
                          (linkedReadings.OutputCapacity > 0
                              ? "/" + linkedReadings.OutputCapacity.ToString(CultureInfo.InvariantCulture) + " ready"
                              : " ready")
                        : "Empty"));

                float nextSeconds;
                if (NativeTimeReader.TryGetSmelterNextEta(linkedReadings, linkedActive, out nextSeconds))
                {
                    etas.Add(new EtaDescriptor("Next output", EtaState.Active, nextSeconds));
                    if (ModConfig.ShowBatchEta.Value && linkedReadings.InputCount > 1)
                        etas.Add(new EtaDescriptor("Batch complete", EtaState.Active,
                            nextSeconds + (linkedReadings.InputCount - 1) * linkedReadings.SecondsPerProduct /
                            Math.Max(0.01f, linkedReadings.Power)));
                }

                if (ModConfig.ShowCapacities.Value && linkedReadings.SecondsPerProduct > 0f)
                {
                    float progress = Mathf.Clamp01(NativeTimeReader.GetSmelterProgress(linkedReadings) /
                        linkedReadings.SecondsPerProduct);
                    lines.Add(new DisplayLine("Progress", (progress * 100f).ToString("0", CultureInfo.InvariantCulture) + "%"));
                }
            }

            string fingerprint = string.Join("|", name, status, power.ToString("0.00", CultureInfo.InvariantCulture),
                hasLinkedSmelter ? linkedReadings.InputName : string.Empty,
                hasLinkedSmelter ? linkedReadings.InputCount.ToString(CultureInfo.InvariantCulture) : "0",
                hasLinkedSmelter ? linkedReadings.OutputCount.ToString(CultureInfo.InvariantCulture) : "0",
                etas.Count > 0 ? NativeTimeReader.VisibleSeconds(etas[0].RemainingSeconds).ToString(CultureInfo.InvariantCulture) : "paused");
            result = new InspectionResult(fingerprint, name, lines, etas, new string[0]);
            return true;
        }

        private static string GetSmelterStatus(SmelterReadings readings, bool active,
            bool hasInput, bool hasFuel, bool hasOutput)
        {
            if (active)
                return "Processing";
            if (hasOutput && !hasInput)
                return "Ready";
            if (hasInput && !hasFuel)
                return "Waiting for fuel";
            if (hasInput)
                return "Paused";
            return "Empty";
        }

        private static string FormatItem(string value)
        {
            if (string.IsNullOrEmpty(value))
                return "Unknown";

            GameObject prefab = ObjectDB.instance ? ObjectDB.instance.GetItemPrefab(value) : null;
            ItemDrop item = prefab ? prefab.GetComponent<ItemDrop>() : null;
            if (item && item.m_itemData != null && item.m_itemData.m_shared != null)
                return SafeText.CleanOrFallback(item.GetHoverName(), Humanize(value));
            return SafeText.CleanOrFallback(value, Humanize(value));
        }

        private static string Humanize(string value)
        {
            string cleaned = SafeText.Clean(value);
            if (cleaned.Length == 0)
                return "Unknown";
            return Regex.Replace(cleaned, "(?<!^)([A-Z])", " $1");
        }

        private static string FormatNumber(float value)
        {
            return value.ToString(Math.Abs(value - Mathf.Round(value)) < 0.01f ? "0" : "0.0",
                CultureInfo.InvariantCulture);
        }

        private static string FormatCapacity(int capacity)
        {
            return capacity > 0 ? "/" + capacity.ToString(CultureInfo.InvariantCulture) : string.Empty;
        }

        private static Smelter ResolveSmelter(GameObject target)
        {
            return target ? target.GetComponentInParent<Smelter>() : null;
        }

        private static Fermenter ResolveFermenter(GameObject target)
        {
            return target ? target.GetComponentInParent<Fermenter>() : null;
        }

        private static Windmill ResolveWindmill(GameObject target)
        {
            return target ? target.GetComponentInParent<Windmill>() : null;
        }
    }
}
