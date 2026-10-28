namespace Template.MobileApp.State;

using System.Diagnostics.CodeAnalysis;

using Template.MobileApp.Components;

public sealed partial class KioskState : ObservableObject, IDisposable
{
    public const string DefaultAdminPin = "0000";

    private readonly ILogger<KioskState> log;

    private readonly ManagedConfiguration managedConfiguration;

    private readonly DeviceOwner deviceOwner;

    private readonly LockTask lockTask;

    private readonly List<IDisposable> disposables = [];

    // Device

    [ObservableProperty]
    public partial bool IsDeviceOwner { get; private set; }

    [ObservableProperty]
    public partial bool IsPolicyApplied { get; private set; }

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

    public KioskState(
        ILogger<KioskState> log,
        ManagedConfiguration managedConfiguration,
        DeviceOwner deviceOwner,
        LockTask lockTask)
    {
        this.log = log;
        this.managedConfiguration = managedConfiguration;
        this.deviceOwner = deviceOwner;
        this.lockTask = lockTask;

        IsDeviceOwner = deviceOwner.IsDeviceOwner;
        RefreshPolicy();
        RefreshLockTask();
        UpdateConfiguration(managedConfiguration.Settings);

        disposables.Add(managedConfiguration.ChangedAsObservable()
            .Select(_ => managedConfiguration.Settings)
            .ObserveOnCurrentContext()
            .Subscribe(UpdateConfiguration));

        disposables.Add(lockTask.ChangedAsObservable()
            .ObserveOnCurrentContext()
            .Subscribe(_ => RefreshLockTask()));
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
        RefreshPolicy();
        RefreshLockTask();
        managedConfiguration.Reload();
    }

    // ------------------------------------------------------------
    // Policy
    // ------------------------------------------------------------

    public void RefreshPolicy()
    {
        var applied = deviceOwner.IsPolicyApplied;
        if (applied != IsPolicyApplied)
        {
            log.InfoDevicePolicyStateChanged(applied);
        }

        IsPolicyApplied = applied;
    }

    // ------------------------------------------------------------
    // Lock task
    // ------------------------------------------------------------

    public void RefreshLockTask()
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

    [MemberNotNull(nameof(AdminPin))]
    private void UpdateConfiguration(ManagedSettings settings)
    {
        log.DebugManagedConfiguration(settings.AdminPin is not null, settings.KeepScreenOn, settings.HideSystemBars, settings.Message);

        AdminPin = settings.AdminPin ?? DefaultAdminPin;
        IsAdminPinConfigured = settings.AdminPin is not null;
        KeepScreenOn = settings.KeepScreenOn ?? true;
        IsKeepScreenOnConfigured = settings.KeepScreenOn is not null;
        HideSystemBars = settings.HideSystemBars ?? true;
        IsHideSystemBarsConfigured = settings.HideSystemBars is not null;
        Message = settings.Message;
    }
}
