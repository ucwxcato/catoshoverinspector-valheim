using System;

namespace CatosHoverInspector
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                AssertEqual("Ready", EtaFormatter.FormatDuration(0L), "zero duration");
                AssertEqual("Ready", EtaFormatter.FormatDuration(-5L), "negative duration");
                AssertEqual("1s", EtaFormatter.FormatDuration(1L), "one second");
                AssertEqual("1m 01s", EtaFormatter.FormatDuration(61L), "minute formatting");
                AssertEqual("1h 00m", EtaFormatter.FormatDuration(3600L), "hour formatting");
                AssertEqual("1d 00h", EtaFormatter.FormatDuration(86400L), "day formatting");

                string text;
                AssertTrue(EtaFormatter.TryFormat(
                    new EtaDescriptor("Next output", EtaState.Active, 1.01f), out text),
                    "active ETA formats");
                AssertEqual("Next output: 2s", text, "active ETA rounds up");

                AssertTrue(EtaFormatter.TryFormat(
                    new EtaDescriptor("Next output", EtaState.Paused, 25f), out text),
                    "paused ETA formats");
                AssertEqual("Next output: Paused", text, "paused ETA does not count down");

                AssertTrue(EtaFormatter.TryFormat(
                    new EtaDescriptor("Ready in", EtaState.Ready, 0f), out text),
                    "ready ETA formats");
                AssertEqual("Ready in: Ready", text, "ready ETA is not zero seconds");

                AssertTrue(EtaFormatter.TryFormat(
                    new EtaDescriptor("Next output", EtaState.Active, -1f), out text),
                    "negative active ETA formats as ready");
                AssertEqual("Next output: Ready", text, "negative ETA clamps to ready");

                AssertTrue(!EtaFormatter.TryFormat(
                    new EtaDescriptor("Next output", EtaState.Unavailable, 10f), out text),
                    "unavailable ETA stays hidden");

                Console.WriteLine("ETA formatter tests passed.");
                return 0;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine(exception.Message);
                return 1;
            }
        }

        private static void AssertTrue(bool condition, string name)
        {
            if (!condition)
                throw new InvalidOperationException("Failed: " + name);
        }

        private static void AssertEqual(string expected, string actual, string name)
        {
            if (!string.Equals(expected, actual, StringComparison.Ordinal))
                throw new InvalidOperationException("Failed: " + name +
                    ". Expected '" + expected + "', got '" + actual + "'.");
        }
    }
}
