namespace Template.MobileApp.Components;

using Android.App;
using Android.Content;
using Android.Content.PM;

public sealed partial class AppUpdater
{
    private static PackageInfo? GetPackageArchiveInfo(PackageManager packageManager, string path) =>
        OperatingSystem.IsAndroidVersionAtLeast(33)
            ? packageManager.GetPackageArchiveInfo(path, PackageManager.PackageInfoFlags.Of(0L))
            : packageManager.GetPackageArchiveInfo(path, 0);

    private static PackageInfo GetPackageInfo(PackageManager packageManager, string packageName) =>
        OperatingSystem.IsAndroidVersionAtLeast(33)
            ? packageManager.GetPackageInfo(packageName, PackageManager.PackageInfoFlags.Of(0L))
            : packageManager.GetPackageInfo(packageName, 0)!;

    private static partial AppUpdateResult PlatformCheckPackage(string path)
    {
        var packageName = Application.Context.PackageName!;
        var packageManager = Application.Context.PackageManager!;
        using var archive = GetPackageArchiveInfo(packageManager, path);
        if ((archive is null) || (archive.PackageName != packageName))
        {
            return AppUpdateResult.InvalidPackage;
        }

        using var installed = GetPackageInfo(packageManager, packageName);
        return archive.LongVersionCode > installed.LongVersionCode ? AppUpdateResult.Downloaded : AppUpdateResult.UpToDate;
    }

    private static async partial Task<bool> PlatformInstallAsync(string path)
    {
        try
        {
            var installer = Application.Context.PackageManager!.PackageInstaller;
            using var parameters = new PackageInstaller.SessionParams(PackageInstallMode.FullInstall);
            parameters.SetAppPackageName(Application.Context.PackageName);
            var sessionId = installer.CreateSession(parameters);
            using var session = installer.OpenSession(sessionId);
            await using (var input = File.OpenRead(path))
            await using (var output = session.OpenWrite(FileName, 0, input.Length))
            {
                await input.CopyToAsync(output);
                session.Fsync(output);
            }

            var flags = OperatingSystem.IsAndroidVersionAtLeast(31) ? PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Mutable : PendingIntentFlags.UpdateCurrent;
            using var intent = new Intent(Application.Context, typeof(AppUpdateReceiver));
            using var pendingIntent = PendingIntent.GetBroadcast(Application.Context, sessionId, intent, flags)!;
            session.Commit(pendingIntent.IntentSender);
            return true;
        }
        catch (Java.IO.IOException)
        {
            return false;
        }
    }
}
