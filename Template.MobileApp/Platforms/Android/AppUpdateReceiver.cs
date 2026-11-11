#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Template.MobileApp;

using Android.App;
using Android.Content;
using Android.Content.PM;

using Template.MobileApp.Components;

[BroadcastReceiver(Name = "template.mobileapp.AppUpdateReceiver", Exported = false)]
[IntentFilter([Intent.ActionMyPackageReplaced])]
public sealed class AppUpdateReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if ((context is null) || (intent is null))
        {
            return;
        }

        if (intent.Action == Intent.ActionMyPackageReplaced)
        {
            using var launch = new Intent(context, typeof(MainActivity));
            launch.AddFlags(ActivityFlags.NewTask);
            context.StartActivity(launch);
            return;
        }

        var status = intent.GetIntExtra(PackageInstaller.ExtraStatus, (int)PackageInstallStatus.Failure);
        var services = IPlatformApplication.Current?.Services;
        services?.GetService<ILogger<AppUpdateReceiver>>()?.InfoAppUpdateStatus(status, intent.GetStringExtra(PackageInstaller.ExtraStatusMessage));
        if (status != (int)PackageInstallStatus.Success)
        {
            services?.GetService<AppUpdater>()?.NotifyInstallFailed();
        }
    }
}
