namespace Template.MobileApp.Modules.Kiosk;

using Template.MobileApp.Components;

public sealed class KioskLockTaskViewModel : AppViewModelBase
{
    // Field

    private static readonly TimeSpan LockTaskPollInterval = TimeSpan.FromSeconds(1);

    private readonly DeviceOwner deviceOwner;

    private readonly LockTask lockTask;

    // Property

    public KioskState Kiosk { get; }

    // Command

    public IObserveCommand StartCommand { get; }

    public IObserveCommand StopCommand { get; }

    public IObserveCommand OpenSettingsCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public KioskLockTaskViewModel(
        DeviceOwner deviceOwner,
        LockTask lockTask,
        KioskState kiosk)
    {
        this.deviceOwner = deviceOwner;
        this.lockTask = lockTask;
        Kiosk = kiosk;

        StartCommand = MakeDelegateCommand(StartLockTask, () => Kiosk.LockTaskState == LockTaskState.None);
        StopCommand = MakeAsyncCommand(StopLockTaskAsync, () => Kiosk.LockTaskState != LockTaskState.None);
        OpenSettingsCommand = MakeDelegateCommand(AndroidHelper.OpenSettings, () => Kiosk.LockTaskState == LockTaskState.None);

        var lockTaskStateChanged = Kiosk.AsObservable(nameof(KioskState.LockTaskState));
        Observe(lockTaskStateChanged, StartCommand);
        Observe(lockTaskStateChanged, StopCommand);
        Observe(lockTaskStateChanged, OpenSettingsCommand);

        Disposables.Add(Observable.Interval(LockTaskPollInterval).ObserveOnCurrentContext().Subscribe(_ => Kiosk.RefreshLockTask()));
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
            deviceOwner.AllowLockTask(Kiosk.LockTaskPackages);
        }

        lockTask.Start();
    }

    private async Task StopLockTaskAsync()
    {
        if (await lockTask.StopAsync() && Kiosk.IsDeviceOwner)
        {
            deviceOwner.DisallowLockTask();
        }
    }
}
