#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Template.MobileApp;

using Android.App;
using Android.Content;

public static class AndroidHelper
{
    public static string GetExternalFilesDir() =>
        Application.Context.GetExternalFilesDir(string.Empty)?.Path ?? Application.Context.FilesDir!.Path;

    public static void MoveTaskToBack() =>
        ActivityResolver.CurrentActivity.MoveTaskToBack(true);

    public static void OpenSettings()
    {
        using var intent = new Intent(Android.Provider.Settings.ActionSettings);
        intent.AddFlags(ActivityFlags.NewTask);
        Application.Context.StartActivity(intent);
    }

    public static string? FindAppLabel(string packageName)
    {
        var packageManager = Application.Context.PackageManager!;
        using var intent = packageManager.GetLaunchIntentForPackage(packageName);
        using var info = intent?.ResolveActivityInfo(packageManager, 0);
        return info?.LoadLabel(packageManager);
    }

    public static bool LaunchApp(string packageName)
    {
        using var intent = Application.Context.PackageManager!.GetLaunchIntentForPackage(packageName);
        if (intent is null)
        {
            return false;
        }

        intent.AddFlags(ActivityFlags.NewTask);
        Application.Context.StartActivity(intent);
        return true;
    }
}
