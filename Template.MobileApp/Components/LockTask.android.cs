namespace Template.MobileApp.Components;

using Android.App;
using Android.App.Admin;
using Android.Content;

public sealed partial class LockTask
{
    private readonly ActivityManager activityManager =
        (ActivityManager)Application.Context.GetSystemService(Context.ActivityService)!;

    private readonly DevicePolicyManager policyManager =
        (DevicePolicyManager)Application.Context.GetSystemService(Context.DevicePolicyService)!;

    public LockTask()
    {
        timer = new Timer(_ => Poll(), null, Timeout.InfiniteTimeSpan, Timeout.InfiniteTimeSpan);
        KioskAdminReceiver.LockTaskModeChanged += OnLockTaskModeChanged;
    }

    public void Dispose()
    {
        KioskAdminReceiver.LockTaskModeChanged -= OnLockTaskModeChanged;

        timer.Dispose();
        activityManager.Dispose();
        policyManager.Dispose();
    }

    private void OnLockTaskModeChanged(object? sender, EventArgs e) => RaiseChanged();

    private partial LockTaskState ResolveState() =>
        activityManager.LockTaskModeState switch
        {
            LockTaskMode.Locked => LockTaskState.Locked,
            LockTaskMode.Pinned => LockTaskState.Pinned,
            _ => LockTaskState.None
        };

    private partial bool ResolvePermitted() =>
        policyManager.IsLockTaskPermitted(Application.Context.PackageName);

    private static partial void PlatformStart() => ActivityResolver.CurrentActivity.StartLockTask();

    private static partial void PlatformStop() => ActivityResolver.CurrentActivity.StopLockTask();
}
