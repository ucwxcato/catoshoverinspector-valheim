namespace CatosHoverInspector
{
    internal enum EtaState
    {
        Unavailable,
        Active,
        Paused,
        Ready
    }

    internal struct EtaDescriptor
    {
        internal EtaDescriptor(string label, EtaState state, float remainingSeconds)
        {
            Label = label ?? string.Empty;
            State = state;
            RemainingSeconds = remainingSeconds;
        }

        internal string Label { get; }
        internal EtaState State { get; }
        internal float RemainingSeconds { get; }

        internal bool IsAvailable
        {
            get { return State != EtaState.Unavailable; }
        }
    }
}
