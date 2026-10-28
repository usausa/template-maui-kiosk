#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Template.MobileApp;

using Android.App;
using Android.App.Admin;
using Android.Content;

[BroadcastReceiver(Name = "template.mobileapp.KioskAdminReceiver", Exported = true, Permission = "android.permission.BIND_DEVICE_ADMIN")]
[MetaData("android.app.device_admin", Resource = "@xml/device_admin")]
[IntentFilter(["android.app.action.DEVICE_ADMIN_ENABLED"])]
public sealed class KioskAdminReceiver : DeviceAdminReceiver
{
    public static event EventHandler? LockTaskModeChanged;

    public override void OnLockTaskModeEntering(Context context, Intent intent, string pkg) =>
        LockTaskModeChanged?.Invoke(null, EventArgs.Empty);

    public override void OnLockTaskModeExiting(Context context, Intent intent) =>
        LockTaskModeChanged?.Invoke(null, EventArgs.Empty);
}
