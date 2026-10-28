namespace Template.MobileApp.Modules.Kiosk;

using Template.MobileApp.Components;

public sealed partial class KioskAdminViewModel : AppViewModelBase
{
    // Field

    private readonly ILogger<KioskAdminViewModel> log;

    private readonly IDialog dialog;

    private readonly DeviceOwner deviceOwner;

    private readonly LockTask lockTask;

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

    public IObserveCommand StartCommand { get; }

    public IObserveCommand StopCommand { get; }

    public IObserveCommand OpenSettingsCommand { get; }

    public IObserveCommand ApplyPolicyCommand { get; }

    public IObserveCommand ClearPolicyCommand { get; }

    public IObserveCommand RebootCommand { get; }

    public IObserveCommand SendCommand { get; }

    public IObserveCommand ClearCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public KioskAdminViewModel(
        ILogger<KioskAdminViewModel> log,
        IDialog dialog,
        DeviceOwner deviceOwner,
        LockTask lockTask,
        KioskState kiosk)
    {
        this.log = log;
        this.dialog = dialog;
        this.deviceOwner = deviceOwner;
        this.lockTask = lockTask;
        Kiosk = kiosk;

        AdminPin = kiosk.IsAdminPinConfigured ? kiosk.AdminPin : string.Empty;
        Message = kiosk.Message ?? string.Empty;
        KeepScreenOn = kiosk.KeepScreenOn;
        HideSystemBars = kiosk.HideSystemBars;

        StartCommand = MakeDelegateCommand(StartLockTask, () => Kiosk.LockTaskState == LockTaskState.None);
        StopCommand = MakeAsyncCommand(StopLockTaskAsync, () => Kiosk.LockTaskState != LockTaskState.None);
        OpenSettingsCommand = MakeDelegateCommand(AndroidHelper.OpenSettings, () => Kiosk.LockTaskState == LockTaskState.None);
        ApplyPolicyCommand = MakeAsyncCommand(ApplyPolicyAsync, () => !Kiosk.IsPolicyApplied);
        ClearPolicyCommand = MakeAsyncCommand(ClearPolicyAsync, () => Kiosk.IsPolicyApplied);
        RebootCommand = MakeAsyncCommand(RebootAsync);
        SendCommand = MakeAsyncCommand(SendAsync);
        ClearCommand = MakeAsyncCommand(ClearAsync);

        var lockTaskStateChanged = Kiosk.AsObservable(nameof(KioskState.LockTaskState));
        Observe(lockTaskStateChanged, StartCommand);
        Observe(lockTaskStateChanged, StopCommand);
        Observe(lockTaskStateChanged, OpenSettingsCommand);

        var policyAppliedChanged = Kiosk.AsObservable(nameof(KioskState.IsPolicyApplied));
        Observe(policyAppliedChanged, ApplyPolicyCommand);
        Observe(policyAppliedChanged, ClearPolicyCommand);
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

    private void StartLockTask()
    {
        if (Kiosk.IsDeviceOwner)
        {
            deviceOwner.AllowLockTask(true);
        }

        lockTask.Start();
    }

    private async Task StopLockTaskAsync()
    {
        if (await lockTask.StopAsync() && Kiosk.IsDeviceOwner)
        {
            deviceOwner.AllowLockTask(false);
        }
    }

    private async Task ApplyPolicyAsync()
    {
        if (!Kiosk.IsDeviceOwner)
        {
            return;
        }

        deviceOwner.ApplyPolicy();
        Kiosk.RefreshPolicy();
        if (Kiosk.LockTaskState == LockTaskState.None)
        {
            lockTask.Start();
        }

        await dialog.Toast("Policy applied");
    }

    private async Task ClearPolicyAsync()
    {
        if (!Kiosk.IsDeviceOwner || !await lockTask.StopAsync())
        {
            return;
        }

        deviceOwner.ClearPolicy();
        Kiosk.RefreshPolicy();
        await dialog.Toast("Policy cleared");
    }

    private async Task RebootAsync()
    {
        if (!Kiosk.IsDeviceOwner || !await dialog.ConfirmAsync("Reboot the device?"))
        {
            return;
        }

        log.InfoRebootRequested();
        deviceOwner.Reboot();
    }

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
