namespace Template.MobileApp.Modules.Kiosk;

public sealed record KioskApp(
    string PackageName,
    string Name,
    bool IsInstalled);

public sealed partial class KioskAppsViewModel : AppViewModelBase
{
    // Field

    private readonly ILogger<KioskAppsViewModel> log;

    private readonly KioskState kiosk;

    // Property

    [ObservableProperty]
    public partial IReadOnlyList<KioskApp> Apps { get; private set; } = [];

    // Command

    public IObserveCommand LaunchCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public KioskAppsViewModel(
        ILogger<KioskAppsViewModel> log,
        KioskState kiosk)
    {
        this.log = log;
        this.kiosk = kiosk;

        LaunchCommand = MakeDelegateCommand<KioskApp>(Launch);
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    public override Task OnNavigatingToAsync(INavigationContext context)
    {
        Apps = kiosk.LockTaskPackages
            .Select(static x => AndroidHelper.FindAppLabel(x) is { } label ? new KioskApp(x, label, true) : new KioskApp(x, x, false))
            .ToList();
        return Task.CompletedTask;
    }

    protected override Task OnNotifyBackAsync() => Navigator.ForwardAsync(ViewId.Menu);

    protected override Task OnNotifyFunction1() => OnNotifyBackAsync();

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private void Launch(KioskApp app) =>
        log.InfoAppLaunched(app.PackageName, AndroidHelper.LaunchApp(app.PackageName));
}
