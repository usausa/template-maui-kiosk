namespace Template.MobileApp.Components;

using Android.App;
using Android.App.Admin;
using Android.Content;
using Android.OS;

public sealed partial class DeviceOwner
{
    private readonly DevicePolicyManager policyManager =
        (DevicePolicyManager)Application.Context.GetSystemService(Context.DevicePolicyService)!;

    public void Dispose() => policyManager.Dispose();

    private static ComponentName CreateAdmin() =>
        new(Application.Context, Java.Lang.Class.FromType(typeof(KioskAdminReceiver)));

    private partial bool ResolveDeviceOwner() =>
        policyManager.IsDeviceOwnerApp(Application.Context.PackageName);

    private partial void PlatformAllowLockTask(bool allow)
    {
        using var admin = CreateAdmin();
        policyManager.SetLockTaskPackages(admin, allow ? [Application.Context.PackageName!] : []);
    }

    private partial void PlatformSendConfiguration(ManagedSettings settings)
    {
        using var admin = CreateAdmin();
        using var bundle = new Bundle();
        if (settings.AdminPin is not null)
        {
            bundle.PutString(ManagedConfiguration.AdminPinKey, settings.AdminPin);
        }

        if (settings.KeepScreenOn is { } keepScreenOn)
        {
            bundle.PutBoolean(ManagedConfiguration.KeepScreenOnKey, keepScreenOn);
        }

        if (settings.HideSystemBars is { } hideSystemBars)
        {
            bundle.PutBoolean(ManagedConfiguration.HideSystemBarsKey, hideSystemBars);
        }

        if (settings.Message is not null)
        {
            bundle.PutString(ManagedConfiguration.MessageKey, settings.Message);
        }

        policyManager.SetApplicationRestrictions(admin, Application.Context.PackageName, bundle);
    }
}
