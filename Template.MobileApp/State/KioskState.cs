namespace Template.MobileApp.State;

using System.Diagnostics.CodeAnalysis;

using Template.MobileApp.Components;

public sealed partial class KioskState : ObservableObject, IDisposable
{
    public const string DefaultAdminPin = "0000";

    private readonly ILogger<KioskState> log;

    private readonly ManagedConfiguration managedConfiguration;

    private readonly DeviceOwner deviceOwner;

    private readonly IDisposable subscription;

    // Device

    [ObservableProperty]
    public partial bool IsDeviceOwner { get; private set; }

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
        DeviceOwner deviceOwner)
    {
        this.log = log;
        this.managedConfiguration = managedConfiguration;
        this.deviceOwner = deviceOwner;

        IsDeviceOwner = deviceOwner.IsDeviceOwner;
        UpdateConfiguration(managedConfiguration.Settings);

        subscription = managedConfiguration.ChangedAsObservable()
            .Select(_ => managedConfiguration.Settings)
            .ObserveOnCurrentContext()
            .Subscribe(UpdateConfiguration);
    }

    public void Dispose()
    {
        subscription.Dispose();
    }

    public void Refresh()
    {
        IsDeviceOwner = deviceOwner.IsDeviceOwner;
        managedConfiguration.Reload();
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
