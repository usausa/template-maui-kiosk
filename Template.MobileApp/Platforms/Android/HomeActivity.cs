#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Template.MobileApp;

using Android.App;
using Android.Content;
using Android.OS;

[Activity(
    Name = "template.mobileapp.HomeActivity",
    Theme = "@android:style/Theme.NoDisplay",
    TaskAffinity = "",
    Enabled = false,
    Exported = true)]
[IntentFilter([Intent.ActionMain], Categories = [Intent.CategoryHome, Intent.CategoryDefault])]
public sealed class HomeActivity : Activity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        using var intent = new Intent(this, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.NewTask);
        StartActivity(intent);
        Finish();
    }
}
