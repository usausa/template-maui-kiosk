namespace Template.MobileApp.Components;

using Android.App;
using Android.App.Admin;
using Android.Content;
using Android.Content.PM;
using Android.OS;

public sealed partial class DeviceOwner
{
    private const int StayOnPluggedIn = (int)BatteryPlugged.Ac | (int)BatteryPlugged.Usb | (int)BatteryPlugged.Wireless;

    private const int StayOnNever = 0;

    private static readonly string[] UserRestrictions =
    [
        UserManager.DisallowSafeBoot,
        UserManager.DisallowFactoryReset,
        UserManager.DisallowAddUser,
        UserManager.DisallowMountPhysicalMedia
    ];

    private readonly DevicePolicyManager policyManager =
        (DevicePolicyManager)Application.Context.GetSystemService(Context.DevicePolicyService)!;

    public void Dispose() => policyManager.Dispose();

    private static ComponentName CreateAdmin() =>
        new(Application.Context, Java.Lang.Class.FromType(typeof(KioskAdminReceiver)));

    private static ComponentName CreateHome() =>
        new(Application.Context, Java.Lang.Class.FromType(typeof(HomeActivity)));

    private partial bool ResolveDeviceOwner() =>
        policyManager.IsDeviceOwnerApp(Application.Context.PackageName);

    private partial bool ResolvePolicyApplied()
    {
        if (!ResolveDeviceOwner())
        {
            return false;
        }

        using var home = CreateHome();
        return Application.Context.PackageManager!.GetComponentEnabledSetting(home) == ComponentEnabledState.Enabled;
    }

    private partial void PlatformAllowLockTask(bool allow)
    {
        using var admin = CreateAdmin();
        policyManager.SetLockTaskPackages(admin, allow ? [Application.Context.PackageName!] : []);
    }

    private partial void PlatformApplyPolicy()
    {
        using var admin = CreateAdmin();
        using var home = CreateHome();
        using var filter = new IntentFilter(Intent.ActionMain);
        filter.AddCategory(Intent.CategoryHome);
        filter.AddCategory(Intent.CategoryDefault);

        policyManager.SetLockTaskPackages(admin, [Application.Context.PackageName!]);
        policyManager.SetLockTaskFeatures(admin, LockTaskFeatures.SystemInfo | LockTaskFeatures.GlobalActions);
        Application.Context.PackageManager!.SetComponentEnabledSetting(home, ComponentEnabledState.Enabled, ComponentEnableOption.DontKillApp);
        policyManager.AddPersistentPreferredActivity(admin, filter, home);
        policyManager.SetKeyguardDisabled(admin, true);
        policyManager.SetGlobalSetting(admin, Android.Provider.Settings.Global.StayOnWhilePluggedIn, StayOnPluggedIn.ToString(CultureInfo.InvariantCulture));
        foreach (var restriction in UserRestrictions)
        {
            policyManager.AddUserRestriction(admin, restriction);
        }
    }

    private partial void PlatformClearPolicy()
    {
        using var admin = CreateAdmin();
        using var home = CreateHome();

        policyManager.SetLockTaskPackages(admin, []);
        policyManager.SetLockTaskFeatures(admin, LockTaskFeatures.GlobalActions);
        Application.Context.PackageManager!.SetComponentEnabledSetting(home, ComponentEnabledState.Default, ComponentEnableOption.DontKillApp);
        policyManager.ClearPackagePersistentPreferredActivities(admin, Application.Context.PackageName);
        policyManager.SetKeyguardDisabled(admin, false);
        policyManager.SetGlobalSetting(admin, Android.Provider.Settings.Global.StayOnWhilePluggedIn, StayOnNever.ToString(CultureInfo.InvariantCulture));
        foreach (var restriction in UserRestrictions)
        {
            policyManager.ClearUserRestriction(admin, restriction);
        }
    }

    private partial void PlatformReboot()
    {
        using var admin = CreateAdmin();
        policyManager.Reboot(admin);
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
