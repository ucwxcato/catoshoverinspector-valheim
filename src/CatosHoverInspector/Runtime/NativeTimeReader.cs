using System;
using System.Reflection;
using HarmonyLib;

namespace CatosHoverInspector
{
    internal struct SmelterReadings
    {
        internal string InputName;
        internal int InputCount;
        internal int InputCapacity;
        internal float Fuel;
        internal int FuelCapacity;
        internal float SecondsPerProduct;
        internal float BakeTimer;
        internal float Accumulator;
        internal int OutputCount;
        internal int OutputCapacity;
        internal bool Active;
        internal float Power;
        internal double NativeElapsedSeconds;
        internal bool HasNativeElapsed;
    }

    internal struct FermenterReadings
    {
        internal int Status;
        internal int Content;
        internal string ContentName;
        internal double ElapsedSeconds;
        internal float Duration;
    }

    internal static class NativeTimeReader
    {
        private static readonly MethodInfo SmelterGetBakeTimer =
            AccessTools.Method(typeof(Smelter), "GetBakeTimer");
        private static readonly MethodInfo SmelterGetFuel =
            AccessTools.Method(typeof(Smelter), "GetFuel");
        private static readonly MethodInfo SmelterGetQueueSize =
            AccessTools.Method(typeof(Smelter), "GetQueueSize");
        private static readonly MethodInfo SmelterGetQueuedOre =
            AccessTools.Method(typeof(Smelter), "GetQueuedOre");
        private static readonly MethodInfo SmelterGetAccumulator =
            AccessTools.Method(typeof(Smelter), "GetAccumulator");
        private static readonly MethodInfo SmelterGetProcessedQueueSize =
            AccessTools.Method(typeof(Smelter), "GetProcessedQueueSize");
        private static readonly MethodInfo SmelterIsActive =
            AccessTools.Method(typeof(Smelter), "IsActive");
        private static readonly MethodInfo FermenterGetStatus =
            AccessTools.Method(typeof(Fermenter), "GetStatus");
        private static readonly MethodInfo FermenterGetContent =
            AccessTools.Method(typeof(Fermenter), "GetContent");
        private static readonly MethodInfo FermenterGetContentName =
            AccessTools.Method(typeof(Fermenter), "GetContentName");
        private static readonly MethodInfo FermenterGetFermentationTime =
            AccessTools.Method(typeof(Fermenter), "GetFermentationTime");
        private static readonly FieldInfo SmelterNView =
            AccessTools.Field(typeof(Smelter), "m_nview");
        private static readonly FieldInfo WindmillSmelter =
            AccessTools.Field(typeof(Windmill), "m_smelter");
        private static readonly MethodInfo SmelterGetItemConversion =
            AccessTools.Method(typeof(Smelter), "GetItemConversion");

        internal static bool TryReadSmelter(Smelter smelter, out SmelterReadings readings)
        {
            readings = new SmelterReadings();
            if (smelter == null || SmelterGetBakeTimer == null || SmelterGetFuel == null ||
                SmelterGetQueueSize == null || SmelterGetQueuedOre == null ||
                SmelterGetAccumulator == null || SmelterGetProcessedQueueSize == null ||
                SmelterIsActive == null)
                return false;

            if (!TryInvoke(SmelterGetBakeTimer, smelter, out readings.BakeTimer) ||
                !TryInvoke(SmelterGetFuel, smelter, out readings.Fuel) ||
                !TryInvoke(SmelterGetQueueSize, smelter, out readings.InputCount) ||
                !TryInvoke(SmelterGetQueuedOre, smelter, out readings.InputName) ||
                !TryInvoke(SmelterGetAccumulator, smelter, out readings.Accumulator) ||
                !TryInvoke(SmelterGetProcessedQueueSize, smelter, out readings.OutputCount) ||
                !TryInvoke(SmelterIsActive, smelter, out readings.Active))
                return false;

            readings.InputCapacity = Math.Max(0, smelter.m_maxOre);
            readings.FuelCapacity = Math.Max(0, smelter.m_maxFuel);
            readings.SecondsPerProduct = Math.Max(0f, smelter.m_secPerProduct);
            readings.Power = smelter.m_windmill ? smelter.m_windmill.GetPowerOutput() : 1f;
            readings.OutputCapacity = TryGetOutputCapacity(smelter, readings.InputName);
            readings.HasNativeElapsed = TryGetNativeElapsedSeconds(smelter, out readings.NativeElapsedSeconds);
            return true;
        }

        internal static bool TryGetLinkedSmelter(Windmill windmill, out Smelter smelter)
        {
            smelter = null;
            if (windmill == null || WindmillSmelter == null)
                return false;

            smelter = WindmillSmelter.GetValue(windmill) as Smelter;
            return smelter;
        }

        internal static float GetSmelterProgress(SmelterReadings readings)
        {
            float progress = Math.Max(0f, readings.BakeTimer);
            if (readings.HasNativeElapsed)
                progress += (float)(readings.NativeElapsedSeconds + Math.Max(0f, readings.Accumulator)) * readings.Power;
            return progress;
        }

        internal static bool TryGetSmelterNextEta(SmelterReadings readings, bool active, out float seconds)
        {
            seconds = 0f;
            if (!active || readings.Power <= 0.01f || readings.SecondsPerProduct <= 0f)
                return false;

            seconds = (readings.SecondsPerProduct - GetSmelterProgress(readings)) / readings.Power;
            if (float.IsNaN(seconds) || float.IsInfinity(seconds))
                return false;
            if (seconds < 0f)
                seconds = 0f;
            return true;
        }

        internal static bool TryReadFermenter(Fermenter fermenter, out FermenterReadings readings)
        {
            readings = new FermenterReadings();
            if (fermenter == null || FermenterGetStatus == null || FermenterGetContent == null ||
                FermenterGetContentName == null || FermenterGetFermentationTime == null)
                return false;

            if (!TryInvokeInt(FermenterGetStatus, fermenter, out readings.Status) ||
                !TryInvoke(FermenterGetContent, fermenter, out readings.Content) ||
                !TryInvoke(FermenterGetContentName, fermenter, out readings.ContentName) ||
                !TryInvoke(FermenterGetFermentationTime, fermenter, out readings.ElapsedSeconds))
                return false;

            readings.Duration = Math.Max(0f, fermenter.m_fermentationDuration);
            return true;
        }

        internal static long VisibleSeconds(float seconds)
        {
            if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0f)
                return 0L;
            return Math.Max(1L, (long)Math.Ceiling(seconds));
        }

        private static bool TryGetNativeElapsedSeconds(Smelter smelter, out double seconds)
        {
            seconds = 0d;
            if (SmelterNView == null || ZNet.instance == null)
                return false;

            ZNetView nview = SmelterNView.GetValue(smelter) as ZNetView;
            if (nview == null || !nview.IsValid() || nview.GetZDO() == null)
                return false;

            long ticks = nview.GetZDO().GetLong(ZDOVars.s_startTime, 0L);
            if (ticks <= 0L)
                return false;

            DateTime start;
            try
            {
                start = new DateTime(ticks);
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }

            seconds = (ZNet.instance.GetTime() - start).TotalSeconds;
            if (seconds < 0d || double.IsNaN(seconds) || double.IsInfinity(seconds))
            {
                seconds = 0d;
                return false;
            }

            // Native Smelter caps its accumulated catch-up window at one hour.
            seconds = Math.Min(seconds, 3600d);
            return true;
        }

        private static int TryGetOutputCapacity(Smelter smelter, string inputName)
        {
            if (smelter == null || !smelter.m_spawnStack || string.IsNullOrEmpty(inputName) ||
                SmelterGetItemConversion == null)
                return 0;

            try
            {
                object conversion = SmelterGetItemConversion.Invoke(smelter, new object[] { inputName });
                if (conversion == null)
                    return 0;

                FieldInfo outputField = AccessTools.Field(conversion.GetType(), "m_to");
                ItemDrop output = outputField == null ? null : outputField.GetValue(conversion) as ItemDrop;
                if (output == null || output.m_itemData == null || output.m_itemData.m_shared == null)
                    return 0;
                return Math.Max(0, output.m_itemData.m_shared.m_maxStackSize);
            }
            catch (TargetInvocationException)
            {
                return 0;
            }
            catch (ArgumentException)
            {
                return 0;
            }
        }

        private static bool TryInvoke<T>(MethodInfo method, object target, out T value)
        {
            value = default(T);
            try
            {
                object raw = method.Invoke(target, null);
                if (raw is T typed)
                {
                    value = typed;
                    return true;
                }
            }
            catch (TargetInvocationException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }

            return false;
        }

        private static bool TryInvokeInt(MethodInfo method, object target, out int value)
        {
            value = 0;
            try
            {
                object raw = method.Invoke(target, null);
                if (raw == null)
                    return false;
                value = Convert.ToInt32(raw, System.Globalization.CultureInfo.InvariantCulture);
                return true;
            }
            catch (TargetInvocationException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidCastException)
            {
                return false;
            }
        }
    }
}
