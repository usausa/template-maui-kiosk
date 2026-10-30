#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Template.MobileApp;

using Android.App;
using Android.App.Admin;
using Android.Content;
using Android.OS;

[Activity(
    Name = "template.mobileapp.ProvisioningModeActivity",
    Theme = "@android:style/Theme.NoDisplay",
    Exported = true,
    Permission = "android.permission.BIND_DEVICE_ADMIN")]
[IntentFilter([DevicePolicyManager.ActionGetProvisioningMode], Categories = [Intent.CategoryDefault])]
public sealed class ProvisioningModeActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        using var result = new Intent();
        result.PutExtra(DevicePolicyManager.ExtraProvisioningMode, DevicePolicyManager.ProvisioningModeFullyManagedDevice);
        SetResult(Result.Ok, result);
        Finish();
    }
}
