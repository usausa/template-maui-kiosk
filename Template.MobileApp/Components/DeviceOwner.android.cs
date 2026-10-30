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

    private const string PreviousStayOnKey = "PreviousStayOnWhilePluggedIn";

    private static readonly string[] UserRestrictions =
    [
        UserManager.DisallowSafeBoot,
        UserManager.DisallowFactoryReset,
        UserManager.DisallowAddUser,
        UserManager.DisallowMountPhysicalMedia
    ];

    private readonly DevicePolicyManager policyManager =
        (DevicePolicyManager)Application.Context.GetSystemService(Context.DevicePolicyService)!;

    private readonly IPreferences preferences;

    public DeviceOwner(IPreferences preferences)
    {
        this.preferences = preferences;
    }

    public void Dispose() => policyManager.Dispose();

    private static ComponentName CreateAdmin() =>
        new(Application.Context, Java.Lang.Class.FromType(typeof(KioskAdminReceiver)));

    private static ComponentName CreateHome() =>
        new(Application.Context, Java.Lang.Class.FromType(typeof(HomeActivity)));

    private static string[] CreateLockTaskPackages(IEnumerable<string> lockTaskPackages) =>
        [Application.Context.PackageName!, .. lockTaskPackages.Where(static x => x != Application.Context.PackageName)];

    private static SystemUpdatePolicy? CreateSystemUpdatePolicy(SystemUpdateWindow systemUpdateWindow) =>
        SystemUpdatePolicy.CreateWindowedInstallPolicy(
            (int)systemUpdateWindow.Start.ToTimeSpan().TotalMinutes,
            (int)systemUpdateWindow.End.ToTimeSpan().TotalMinutes);

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

    private partial SystemUpdateWindow? ResolveAppliedSystemUpdateWindow()
    {
        using var policy = policyManager.SystemUpdatePolicy;
        if ((policy is null) || (policy.PolicyType != SystemUpdatePolicyType.InstallWindowed))
        {
            return null;
        }

        return new SystemUpdateWindow(
            TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(policy.InstallWindowStart)),
            TimeOnly.FromTimeSpan(TimeSpan.FromMinutes(policy.InstallWindowEnd)));
    }

    private partial bool ResolveLockTaskAllowed() =>
        policyManager.IsLockTaskPermitted(Application.Context.PackageName);

    private partial void PlatformAllowLockTask(IReadOnlyList<string> lockTaskPackages)
    {
        using var admin = CreateAdmin();
        policyManager.SetLockTaskPackages(admin, CreateLockTaskPackages(lockTaskPackages));
    }

    private partial void PlatformDisallowLockTask()
    {
        using var admin = CreateAdmin();
        policyManager.SetLockTaskPackages(admin, []);
    }

    private partial void PlatformApplyPolicy(SystemUpdateWindow systemUpdateWindow, IReadOnlyList<string> lockTaskPackages)
    {
        using var admin = CreateAdmin();
        using var home = CreateHome();
        using var systemUpdatePolicy = CreateSystemUpdatePolicy(systemUpdateWindow);
        using var filter = new IntentFilter(Intent.ActionMain);
        filter.AddCategory(Intent.CategoryHome);
        filter.AddCategory(Intent.CategoryDefault);

        policyManager.SetLockTaskPackages(admin, CreateLockTaskPackages(lockTaskPackages));
        policyManager.SetLockTaskFeatures(admin, LockTaskFeatures.SystemInfo | LockTaskFeatures.GlobalActions);
        Application.Context.PackageManager!.SetComponentEnabledSetting(home, ComponentEnabledState.Enabled, ComponentEnableOption.DontKillApp);
        policyManager.AddPersistentPreferredActivity(admin, filter, home);
        policyManager.SetKeyguardDisabled(admin, true);
        preferences.Set(PreviousStayOnKey, Android.Provider.Settings.Global.GetInt(Application.Context.ContentResolver, Android.Provider.Settings.Global.StayOnWhilePluggedIn, StayOnNever));
        policyManager.SetGlobalSetting(admin, Android.Provider.Settings.Global.StayOnWhilePluggedIn, StayOnPluggedIn.ToString(CultureInfo.InvariantCulture));
        foreach (var restriction in UserRestrictions)
        {
            policyManager.AddUserRestriction(admin, restriction);
        }

        policyManager.SetSystemUpdatePolicy(admin, systemUpdatePolicy);
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
        policyManager.SetGlobalSetting(admin, Android.Provider.Settings.Global.StayOnWhilePluggedIn, preferences.Get(PreviousStayOnKey, StayOnNever).ToString(CultureInfo.InvariantCulture));
        preferences.Remove(PreviousStayOnKey);
        foreach (var restriction in UserRestrictions)
        {
            policyManager.ClearUserRestriction(admin, restriction);
        }

        policyManager.SetSystemUpdatePolicy(admin, null);
    }

    private partial void PlatformReboot()
    {
        using var admin = CreateAdmin();
        policyManager.Reboot(admin);
    }

    private partial void PlatformSetSystemUpdateWindow(SystemUpdateWindow systemUpdateWindow)
    {
        using var admin = CreateAdmin();
        using var systemUpdatePolicy = CreateSystemUpdatePolicy(systemUpdateWindow);
        policyManager.SetSystemUpdatePolicy(admin, systemUpdatePolicy);
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

        if (settings.SystemUpdateWindow is not null)
        {
            bundle.PutString(ManagedConfiguration.SystemUpdateWindowKey, settings.SystemUpdateWindow);
        }

        if (settings.LockTaskPackages is not null)
        {
            bundle.PutString(ManagedConfiguration.LockTaskPackagesKey, settings.LockTaskPackages);
        }

        policyManager.SetApplicationRestrictions(admin, Application.Context.PackageName, bundle);
    }
}
