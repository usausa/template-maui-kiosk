#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Template.MobileApp;

using Android.App;
using Android.Runtime;

[Application]
[MetaData("android.content.APP_RESTRICTIONS", Resource = "@xml/app_restrictions")]
public sealed class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
