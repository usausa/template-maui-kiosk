namespace Template.MobileApp.Components;

public sealed record ManagedSettings(
    string? AdminPin,
    bool? KeepScreenOn,
    bool? HideSystemBars,
    string? Message,
    string? SystemUpdateWindow,
    string? LockTaskPackages);

public sealed partial class ManagedConfiguration : IDisposable
{
    public const string AdminPinKey = "AdminPin";
    public const string KeepScreenOnKey = "KeepScreenOn";
    public const string HideSystemBarsKey = "HideSystemBars";
    public const string MessageKey = "Message";
    public const string SystemUpdateWindowKey = "SystemUpdateWindow";
    public const string LockTaskPackagesKey = "LockTaskPackages";

    private static readonly ManagedSettings Empty = new(null, null, null, null, null, null);

    private bool started;

    public event EventHandler? Changed;

    public ManagedSettings Settings { get; private set; } = Empty;

    // ------------------------------------------------------------
    // Control
    // ------------------------------------------------------------

    public void Start()
    {
        if (started)
        {
            return;
        }

        started = true;
        Reload();
        StartReceiver();
    }

    public void Stop()
    {
        if (!started)
        {
            return;
        }

        started = false;
        StopReceiver();
    }

    public void Reload()
    {
        var settings = ReadSettings();
        if (settings != Settings)
        {
            Settings = settings;
            Changed?.Invoke(this, EventArgs.Empty);
        }
    }

    // ------------------------------------------------------------
    // Value
    // ------------------------------------------------------------

    public static IReadOnlyList<string> ParseLockTaskPackages(string? value) =>
        value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.Ordinal).ToList() ?? [];

    private partial ManagedSettings ReadSettings();

    private partial void StartReceiver();

    private partial void StopReceiver();
}
