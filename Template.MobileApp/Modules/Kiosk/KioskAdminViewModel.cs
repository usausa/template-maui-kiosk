namespace Template.MobileApp.Modules.Kiosk;

using Template.MobileApp.Components;

public sealed partial class KioskAdminViewModel : AppViewModelBase
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
    public partial bool KeepScreenOn { get; set; }

    [ObservableProperty]
    public partial bool HideSystemBars { get; set; }

    // Command

    public IObserveCommand SendCommand { get; }

    public IObserveCommand ClearCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public KioskAdminViewModel(
        IDialog dialog,
        DeviceOwner deviceOwner,
        KioskState kiosk)
    {
        this.dialog = dialog;
        this.deviceOwner = deviceOwner;
        Kiosk = kiosk;

        AdminPin = kiosk.IsAdminPinConfigured ? kiosk.AdminPin : string.Empty;
        Message = kiosk.Message ?? string.Empty;
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

        deviceOwner.SendConfiguration(new ManagedSettings(
            String.IsNullOrEmpty(AdminPin) ? null : AdminPin,
            KeepScreenOn,
            HideSystemBars,
            String.IsNullOrEmpty(Message) ? null : Message));
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
