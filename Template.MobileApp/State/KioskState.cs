namespace Template.MobileApp.State;

using System.Diagnostics.CodeAnalysis;

using Template.MobileApp.Components;

public sealed partial class KioskState : ObservableObject, IDisposable
{
    public const string DefaultAdminPin = "0000";

    private const string LockTaskKey = "LockTask";

    private const string DevicePolicyKey = "DevicePolicy";

    private readonly ILogger<KioskState> log;

    private readonly ManagedConfiguration managedConfiguration;

    private readonly DeviceOwner deviceOwner;

    private readonly LockTask lockTask;

    private readonly AppFeedback appFeedback;

    private readonly List<IDisposable> disposables = [];

    private IReadOnlyList<AppState> reportedStates = [];

    private bool adminPinValid;

    private bool systemUpdateWindowValid;

    // Device

    [ObservableProperty]
    public partial bool IsDeviceOwner { get; private set; }

    [ObservableProperty]
    public partial bool IsPolicyApplied { get; private set; }

    [ObservableProperty]
    public partial SystemUpdateWindow? AppliedSystemUpdateWindow { get; private set; }

    // Lock task

    [ObservableProperty]
    public partial LockTaskState LockTaskState { get; private set; }

    [ObservableProperty]
    public partial bool IsLockTaskPermitted { get; private set; }

    // Configuration

    [ObservableProperty]
    public partial string AdminPin { get; private set; }

    [ObservableProperty]
    public partial bool IsAdminPinConfigured { get; private set; }

    [ObservableProperty]
    public partial bool KeepScreenOn { get; private set; }

    [ObservableProperty]
    public partial bool IsKeepScreenOnConfigured { get; private set; }

    [ObservableProperty]
    public partial bool HideSystemBars { get; private set; }

    [ObservableProperty]
    public partial bool IsHideSystemBarsConfigured { get; private set; }

    [ObservableProperty]
    public partial string? Message { get; private set; }

    [ObservableProperty]
    public partial SystemUpdateWindow SystemUpdateWindow { get; private set; }

    [ObservableProperty]
    public partial bool IsSystemUpdateWindowConfigured { get; private set; }

    [ObservableProperty]
    public partial IReadOnlyList<string> LockTaskPackages { get; private set; }

    [ObservableProperty]
    public partial bool IsLockTaskPackagesConfigured { get; private set; }

    // Feedback

    [ObservableProperty]
    public partial IReadOnlyList<AppState> ReceivedStates { get; private set; }

    public KioskState(
        ILogger<KioskState> log,
        ManagedConfiguration managedConfiguration,
        DeviceOwner deviceOwner,
        LockTask lockTask,
        AppFeedback appFeedback)
    {
        this.log = log;
        this.managedConfiguration = managedConfiguration;
        this.deviceOwner = deviceOwner;
        this.lockTask = lockTask;
        this.appFeedback = appFeedback;

        IsDeviceOwner = deviceOwner.IsDeviceOwner;
        UpdatePolicy();
        UpdateLockTask();
        UpdateConfiguration(managedConfiguration.Settings);
        ReceivedStates = appFeedback.ReceivedStates;
        ReportStates();

        disposables.Add(managedConfiguration.ChangedAsObservable()
            .Select(_ => managedConfiguration.Settings)
            .ObserveOnCurrentContext()
            .Subscribe(settings =>
            {
                UpdateConfiguration(settings);
                ReportStates();
            }));

        disposables.Add(lockTask.ChangedAsObservable()
            .ObserveOnCurrentContext()
            .Subscribe(_ => RefreshLockTask()));

        disposables.Add(appFeedback.ReceivedAsObservable()
            .ObserveOnCurrentContext()
            .Subscribe(_ => UpdateReceivedStates()));
    }

    public void Dispose()
    {
        foreach (var disposable in disposables)
        {
            disposable.Dispose();
        }

        disposables.Clear();
    }

    public void Refresh()
    {
        IsDeviceOwner = deviceOwner.IsDeviceOwner;
        UpdatePolicy();
        UpdateLockTask();
        ReportStates();
        managedConfiguration.Reload();
    }

    // ------------------------------------------------------------
    // Policy
    // ------------------------------------------------------------

    public void RefreshPolicy()
    {
        UpdatePolicy();
        ReportStates();
    }

    private void UpdatePolicy()
    {
        var applied = deviceOwner.IsPolicyApplied;
        if (applied != IsPolicyApplied)
        {
            log.InfoDevicePolicyStateChanged(applied);
        }

        IsPolicyApplied = applied;
        AppliedSystemUpdateWindow = deviceOwner.AppliedSystemUpdateWindow;
    }

    // ------------------------------------------------------------
    // Lock task
    // ------------------------------------------------------------

    public void RefreshLockTask()
    {
        UpdateLockTask();
        ReportStates();
    }

    private void UpdateLockTask()
    {
        var state = lockTask.State;
        var permitted = lockTask.IsPermitted;
        if ((state != LockTaskState) || (permitted != IsLockTaskPermitted))
        {
            log.InfoLockTaskStateChanged(state, permitted);
        }

        LockTaskState = state;
        IsLockTaskPermitted = permitted;
    }

    // ------------------------------------------------------------
    // Configuration
    // ------------------------------------------------------------

    [MemberNotNull(nameof(AdminPin), nameof(LockTaskPackages))]
    private void UpdateConfiguration(ManagedSettings settings)
    {
        log.DebugManagedConfiguration(settings.AdminPin is not null, settings.KeepScreenOn, settings.HideSystemBars, settings.Message, settings.SystemUpdateWindow, settings.LockTaskPackages);

        AdminPin = settings.AdminPin ?? DefaultAdminPin;
        IsAdminPinConfigured = settings.AdminPin is not null;
        KeepScreenOn = settings.KeepScreenOn ?? true;
        IsKeepScreenOnConfigured = settings.KeepScreenOn is not null;
        HideSystemBars = settings.HideSystemBars ?? true;
        IsHideSystemBarsConfigured = settings.HideSystemBars is not null;
        Message = settings.Message;
        IsSystemUpdateWindowConfigured = SystemUpdateWindow.TryParse(settings.SystemUpdateWindow, out var systemUpdateWindow);
        SystemUpdateWindow = IsSystemUpdateWindowConfigured ? systemUpdateWindow : SystemUpdateWindow.Default;
        LockTaskPackages = ManagedConfiguration.ParseLockTaskPackages(settings.LockTaskPackages);
        IsLockTaskPackagesConfigured = settings.LockTaskPackages is not null;

        adminPinValid = settings.AdminPin?.All(Char.IsAsciiDigit) ?? true;
        systemUpdateWindowValid = (settings.SystemUpdateWindow is null) || IsSystemUpdateWindowConfigured;
    }

    // ------------------------------------------------------------
    // Feedback
    // ------------------------------------------------------------

    private void ReportStates()
    {
        IReadOnlyList<AppState> states =
        [
            new(LockTaskKey, AppStateSeverity.Info, null, LockTaskState.ToString()),
            new(DevicePolicyKey, AppStateSeverity.Info, null, IsPolicyApplied ? "Applied" : "NotApplied"),
            adminPinValid
                ? new(ManagedConfiguration.AdminPinKey, AppStateSeverity.Info, null, null)
                : new(ManagedConfiguration.AdminPinKey, AppStateSeverity.Error, "Admin PIN must be digits", null),
            systemUpdateWindowValid
                ? new(ManagedConfiguration.SystemUpdateWindowKey, AppStateSeverity.Info, null, null)
                : new(ManagedConfiguration.SystemUpdateWindowKey, AppStateSeverity.Error, "System update window must be HH:mm-HH:mm", null)
        ];
        if (states.SequenceEqual(reportedStates))
        {
            return;
        }

        reportedStates = states;
        appFeedback.Report(states);
        log.DebugAppStatesReported(states.Count);
    }

    private void UpdateReceivedStates()
    {
        ReceivedStates = appFeedback.ReceivedStates;
        log.DebugAppStatesReceived(ReceivedStates.Count);
    }
}
