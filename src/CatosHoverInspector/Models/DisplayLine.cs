namespace CatosHoverInspector
{
    internal sealed class DisplayLine
    {
        internal DisplayLine(string label, string value, int priority = 0, bool warning = false)
        {
            Label = label ?? string.Empty;
            Value = value ?? string.Empty;
            Priority = priority;
            IsWarning = warning;
        }

        internal string Label { get; }
        internal string Value { get; }
        internal int Priority { get; }
        internal bool IsWarning { get; }
    }
}
