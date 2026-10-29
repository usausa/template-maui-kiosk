#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Template.MobileApp;

using System.ComponentModel;

using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

using AndroidX.Core.View;

using Template.MobileApp.Components;

[Activity(
    Name = "template.mobileapp.MainActivity",
    Theme = "@style/Maui.SplashTheme",
    MainLauncher = true,
    AlwaysRetainTaskState = true,
    LaunchMode = LaunchMode.SingleTask,
    LockTaskMode = "if_whitelisted",
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density |
                           ConfigChanges.Keyboard | ConfigChanges.KeyboardHidden | ConfigChanges.Navigation | ConfigChanges.Touchscreen |
                           ConfigChanges.Locale | ConfigChanges.LayoutDirection | ConfigChanges.FontScale | ConfigChanges.Mcc | ConfigChanges.Mnc,
    ScreenOrientation = ScreenOrientation.Portrait)]
public sealed class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // Disable overscroll effect
        Theme?.ApplyStyle(_Microsoft.Android.Resource.Designer.ResourceConstant.Style.NoOverScrollTheme, true);

        base.OnCreate(savedInstanceState);

        if (FindKioskState() is { } kioskState)
        {
            kioskState.PropertyChanged += OnKioskStateChanged;
        }

        UpdateKioskWindow();
    }

    protected override void OnDestroy()
    {
        if (FindKioskState() is { } kioskState)
        {
            kioskState.PropertyChanged -= OnKioskStateChanged;
        }

        base.OnDestroy();
    }

    public override void OnWindowFocusChanged(bool hasFocus)
    {
        base.OnWindowFocusChanged(hasFocus);

        if (hasFocus)
        {
            FindKioskState()?.RefreshLockTask();
        }
    }

    private void OnKioskStateChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(KioskState.LockTaskState) or nameof(KioskState.KeepScreenOn) or nameof(KioskState.HideSystemBars))
        {
            UpdateKioskWindow();
        }
    }

    private static KioskState? FindKioskState() =>
        IPlatformApplication.Current?.Services.GetService<KioskState>();

    private void UpdateKioskWindow()
    {
        if ((Window is not { } window) || (FindKioskState() is not { } kioskState))
        {
            return;
        }

        var locked = kioskState.LockTaskState != LockTaskState.None;
        if (locked && kioskState.KeepScreenOn)
        {
            window.AddFlags(WindowManagerFlags.KeepScreenOn);
        }
        else
        {
            window.ClearFlags(WindowManagerFlags.KeepScreenOn);
        }

        using var controller = WindowCompat.GetInsetsController(window, window.DecorView);
        if (controller is null)
        {
            return;
        }

        if (locked && kioskState.HideSystemBars)
        {
            controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
            controller.Hide(WindowInsetsCompat.Type.SystemBars());
        }
        else
        {
            controller.Show(WindowInsetsCompat.Type.SystemBars());
        }
    }
}
