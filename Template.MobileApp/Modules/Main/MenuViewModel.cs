namespace Template.MobileApp.Modules.Main;

using Template.MobileApp.Components;

public sealed class MenuViewModel : AppViewModelBase
{
    // Field

    private const int AdminPinAttempts = 5;

    private const string AdminPinFailuresKey = "AdminPinFailures";

    private const string AdminPinLockedUntilKey = "AdminPinLockedUntil";

    private static readonly TimeSpan AdminPinLockout = TimeSpan.FromSeconds(30);

    private readonly ILogger<MenuViewModel> log;

    private readonly IDialog dialog;

    private readonly IPopupNavigator popupNavigator;

    private readonly IPreferences preferences;

    private readonly TimeProvider timeProvider;

    private readonly DeviceOwner deviceOwner;

    // Property

    public KioskState Kiosk { get; }

    // Command

    public IObserveCommand ForwardCommand { get; }

    public IObserveCommand AdminCommand { get; }

    public IObserveCommand RebootCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public MenuViewModel(
        ILogger<MenuViewModel> log,
        IDialog dialog,
        IPopupNavigator popupNavigator,
        IPreferences preferences,
        TimeProvider timeProvider,
        DeviceOwner deviceOwner,
        KioskState kiosk)
    {
        this.log = log;
        this.dialog = dialog;
        this.popupNavigator = popupNavigator;
        this.preferences = preferences;
        this.timeProvider = timeProvider;
        this.deviceOwner = deviceOwner;
        Kiosk = kiosk;

        ForwardCommand = MakeAsyncCommand<ViewId>(x => Navigator.ForwardAsync(x));
        AdminCommand = MakeAsyncCommand<ViewId>(OpenAdminAsync);
        RebootCommand = MakeAsyncCommand(RebootAsync);
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    protected override Task OnNotifyBackAsync()
    {
        AndroidHelper.MoveTaskToBack();
        return Task.CompletedTask;
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task OpenAdminAsync(ViewId id)
    {
        if (await InputAdminPinAsync())
        {
            await Navigator.ForwardAsync(id);
        }
    }

    private async Task RebootAsync()
    {
        if (!Kiosk.IsDeviceOwner || !await InputAdminPinAsync() || !await dialog.ConfirmAsync("Reboot the device?"))
        {
            return;
        }

        log.InfoRebootRequested();
        deviceOwner.Reboot();
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private async Task<bool> InputAdminPinAsync()
    {
        var lockedUntil = DateTimeOffset.FromUnixTimeMilliseconds(preferences.Get(AdminPinLockedUntilKey, 0L));
        var remaining = lockedUntil - timeProvider.GetUtcNow();
        if (remaining > TimeSpan.Zero)
        {
            await dialog.InformationAsync(String.Create(CultureInfo.InvariantCulture, $"Try again in {Math.Ceiling(remaining.TotalSeconds)} seconds"));
            return false;
        }

        var pin = await popupNavigator.InputPinAsync("Admin PIN", Length.AdminPin);
        if (pin is null)
        {
            return false;
        }

        if (pin != Kiosk.AdminPin)
        {
            var failures = preferences.Get(AdminPinFailuresKey, 0) + 1;
            preferences.Set(AdminPinFailuresKey, failures);
            if (failures % AdminPinAttempts == 0)
            {
                var until = timeProvider.GetUtcNow() + AdminPinLockout;
                preferences.Set(AdminPinLockedUntilKey, until.ToUnixTimeMilliseconds());
                log.WarnAdminPinLocked(failures, until);
                await dialog.InformationAsync(String.Create(CultureInfo.InvariantCulture, $"Try again in {AdminPinLockout.TotalSeconds} seconds"));
                return false;
            }

            await dialog.InformationAsync("PIN is incorrect");
            return false;
        }

        preferences.Remove(AdminPinFailuresKey);
        preferences.Remove(AdminPinLockedUntilKey);
        return true;
    }
}
