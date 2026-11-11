namespace Template.MobileApp.Modules.Kiosk;

using Template.MobileApp.Components;

public sealed partial class KioskConfigurationViewModel : AppViewModelBase
{
    // Field

    private readonly IDialog dialog;

    private readonly DeviceOwner deviceOwner;

    // Property

    public KioskState Kiosk { get; }

    [ObservableProperty]
    public partial string AdminPin { get; set; }

    [ObservableProperty]
    public partial string Message { get; set; }

    [ObservableProperty]
    public partial string UpdateWindow { get; set; }

    [ObservableProperty]
    public partial string LockTaskPackages { get; set; }

    [ObservableProperty]
    public partial string UpdateLocation { get; set; }

    [ObservableProperty]
    public partial string LockScreenInfo { get; set; }

    [ObservableProperty]
    public partial string SupportMessage { get; set; }

    [ObservableProperty]
    public partial bool KeepScreenOn { get; set; }

    [ObservableProperty]
    public partial bool HideSystemBars { get; set; }

    // Command

    public IObserveCommand SendCommand { get; }

    public IObserveCommand ClearCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public KioskConfigurationViewModel(
        IDialog dialog,
        DeviceOwner deviceOwner,
        KioskState kiosk)
    {
        this.dialog = dialog;
        this.deviceOwner = deviceOwner;
        Kiosk = kiosk;

        AdminPin = kiosk.IsAdminPinConfigured ? kiosk.AdminPin : string.Empty;
        Message = kiosk.Message ?? string.Empty;
        UpdateWindow = kiosk.IsSystemUpdateWindowConfigured ? kiosk.SystemUpdateWindow.ToString() : string.Empty;
        LockTaskPackages = kiosk.IsLockTaskPackagesConfigured ? String.Join(',', kiosk.LockTaskPackages) : string.Empty;
        UpdateLocation = kiosk.UpdateLocation ?? string.Empty;
        LockScreenInfo = kiosk.LockScreenInfo ?? string.Empty;
        SupportMessage = kiosk.SupportMessage ?? string.Empty;
        KeepScreenOn = kiosk.KeepScreenOn;
        HideSystemBars = kiosk.HideSystemBars;

        SendCommand = MakeAsyncCommand(SendAsync);
        ClearCommand = MakeAsyncCommand(ClearAsync);
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    public override Task OnNavigatingToAsync(INavigationContext context)
    {
        Kiosk.Refresh();
        return Task.CompletedTask;
    }

    protected override Task OnNotifyBackAsync() => Navigator.ForwardAsync(ViewId.Menu);

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task SendAsync()
    {
        if (!Kiosk.IsDeviceOwner)
        {
            return;
        }

        if (!AdminPin.All(Char.IsAsciiDigit))
        {
            await dialog.InformationAsync("Admin PIN must be digits");
            return;
        }

        if (!String.IsNullOrEmpty(UpdateWindow) && !SystemUpdateWindow.TryParse(UpdateWindow, out _))
        {
            await dialog.InformationAsync("Update window must be HH:mm-HH:mm");
            return;
        }

        if (!String.IsNullOrEmpty(UpdateLocation) && !Uri.TryCreate(UpdateLocation, UriKind.Absolute, out _))
        {
            await dialog.InformationAsync("Update URL must be a URL");
            return;
        }

        deviceOwner.SendConfiguration(new ManagedSettings(
            String.IsNullOrEmpty(AdminPin) ? null : AdminPin,
            KeepScreenOn,
            HideSystemBars,
            String.IsNullOrEmpty(Message) ? null : Message,
            String.IsNullOrEmpty(UpdateWindow) ? null : UpdateWindow,
            String.IsNullOrWhiteSpace(LockTaskPackages) ? null : LockTaskPackages,
            String.IsNullOrEmpty(UpdateLocation) ? null : UpdateLocation,
            String.IsNullOrEmpty(LockScreenInfo) ? null : LockScreenInfo,
            String.IsNullOrEmpty(SupportMessage) ? null : SupportMessage));
        await dialog.Toast("Configuration sent");
    }

    private async Task ClearAsync()
    {
        if (!Kiosk.IsDeviceOwner)
        {
            return;
        }

        deviceOwner.ClearConfiguration();
        await dialog.Toast("Configuration cleared");
    }
}
