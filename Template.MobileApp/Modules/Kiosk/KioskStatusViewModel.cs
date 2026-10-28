namespace Template.MobileApp.Modules.Kiosk;

public sealed class KioskStatusViewModel : AppViewModelBase
{
    // Property

    public KioskState Kiosk { get; }

    public string Version { get; }

    public string Build { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public KioskStatusViewModel(
        IAppInfo appInfo,
        KioskState kiosk)
    {
        Kiosk = kiosk;
        Version = appInfo.VersionString;
        Build = appInfo.BuildString;
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    public override Task OnNavigatingToAsync(INavigationContext context)
    {
        Kiosk.Refresh();
        return Task.CompletedTask;
    }

    protected override Task OnNotifyBackAsync() => Navigator.ForwardAsync(ViewId.Menu);

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();
}
