namespace Template.MobileApp.Modules.Kiosk;

using Template.MobileApp.Components;

public sealed class KioskPolicyViewModel : AppViewModelBase
{
    // Field

    private readonly IDialog dialog;

    private readonly DeviceOwner deviceOwner;

    private readonly LockTask lockTask;

    // Property

    public KioskState Kiosk { get; }

    // Command

    public IObserveCommand ApplyPolicyCommand { get; }

    public IObserveCommand ClearPolicyCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public KioskPolicyViewModel(
        IDialog dialog,
        DeviceOwner deviceOwner,
        LockTask lockTask,
        KioskState kiosk)
    {
        this.dialog = dialog;
        this.deviceOwner = deviceOwner;
        this.lockTask = lockTask;
        Kiosk = kiosk;

        ApplyPolicyCommand = MakeAsyncCommand(ApplyPolicyAsync, () => !Kiosk.IsPolicyApplied);
        ClearPolicyCommand = MakeAsyncCommand(ClearPolicyAsync, () => Kiosk.IsPolicyApplied);

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

    private async Task ApplyPolicyAsync()
    {
        if (!Kiosk.IsDeviceOwner)
        {
            return;
        }

        deviceOwner.ApplyPolicy(Kiosk.SystemUpdateWindow, Kiosk.LockTaskPackages);
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
}
