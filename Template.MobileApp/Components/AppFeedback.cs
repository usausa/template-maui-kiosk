namespace Template.MobileApp.Components;

public enum AppStateSeverity
{
    Info,
    Error
}

public sealed record AppState(
    string Key,
    AppStateSeverity Severity,
    string? Message,
    string? Data);

public sealed partial class AppFeedback : IDisposable
{
    private IReadOnlyList<AppState> receivedStates = [];

    public event EventHandler? Received;

    public IReadOnlyList<AppState> ReceivedStates => Volatile.Read(ref receivedStates);

    // ------------------------------------------------------------
    // Report
    // ------------------------------------------------------------

    public void Report(IReadOnlyList<AppState> states) => PlatformReport(states);

    // ------------------------------------------------------------
    // Receive
    // ------------------------------------------------------------

    public void Receive(IReadOnlyList<AppState> states)
    {
        Volatile.Write(ref receivedStates, states);
        Received?.Invoke(this, EventArgs.Empty);
    }

    private partial void PlatformReport(IEnumerable<AppState> states);
}
