#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Template.MobileApp;

using Android.App;
using Android.App.Admin;
using Android.Content;
using Android.OS;

using AndroidX.Core.Content;

using Template.MobileApp.Components;

[Activity(
    Name = "template.mobileapp.PolicyComplianceActivity",
    Theme = "@android:style/Theme.NoDisplay",
    Exported = true,
    Permission = "android.permission.BIND_DEVICE_ADMIN")]
[IntentFilter([DevicePolicyManager.ActionAdminPolicyCompliance], Categories = [Intent.CategoryDefault])]
public sealed class PolicyComplianceActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        var extras = Intent is not null
            ? IntentCompat.GetParcelableExtra(Intent, DevicePolicyManager.ExtraProvisioningAdminExtrasBundle, Java.Lang.Class.FromType(typeof(PersistableBundle))) as PersistableBundle
            : null;
        var applyPolicy = ReadBoolean(extras, DeviceOwner.ApplyPolicyKey) ?? false;

        var services = IPlatformApplication.Current?.Services;
        services?.GetService<DeviceOwner>()?.CompleteProvisioning(ReadSettings(extras), applyPolicy);
        services?.GetService<ILogger<PolicyComplianceActivity>>()?.InfoProvisioningCompleted(applyPolicy);

        SetResult(Result.Ok);
        Finish();
    }

    private static ManagedSettings ReadSettings(PersistableBundle? extras) =>
        new(
            extras?.GetString(ManagedConfiguration.AdminPinKey),
            ReadBoolean(extras, ManagedConfiguration.KeepScreenOnKey),
            ReadBoolean(extras, ManagedConfiguration.HideSystemBarsKey),
            extras?.GetString(ManagedConfiguration.MessageKey),
            extras?.GetString(ManagedConfiguration.SystemUpdateWindowKey),
            extras?.GetString(ManagedConfiguration.LockTaskPackagesKey));

    private static bool? ReadBoolean(PersistableBundle? extras, string key) =>
        Boolean.TryParse(extras?.GetString(key), out var value) ? value : null;
}
