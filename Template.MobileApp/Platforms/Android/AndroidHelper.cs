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
}
