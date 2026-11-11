namespace Template.MobileApp.Modules.Kiosk;

using Template.MobileApp.Components;

public sealed class KioskUpdateViewModel : AppViewModelBase
{
    // Field

    private readonly ILogger<KioskUpdateViewModel> log;

    private readonly IDialog dialog;

    private readonly AppUpdater appUpdater;

    // Property

    public KioskState Kiosk { get; }

    public string Version { get; }

    // Command

    public IObserveCommand UpdateCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public KioskUpdateViewModel(
        ILogger<KioskUpdateViewModel> log,
        IDialog dialog,
        IAppInfo appInfo,
        AppUpdater appUpdater,
        KioskState kiosk)
    {
        this.log = log;
        this.dialog = dialog;
        this.appUpdater = appUpdater;
        Kiosk = kiosk;
        Version = $"{appInfo.VersionString} ({appInfo.BuildString})";

        UpdateCommand = MakeAsyncCommand(UpdateAsync, () => Kiosk.UpdateLocation is not null);

        Observe(Kiosk.AsObservable(nameof(KioskState.UpdateLocation)), UpdateCommand);
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

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task UpdateAsync()
    {
        if (!Kiosk.IsDeviceOwner || (Kiosk.UpdateLocation is not { } location) || !await dialog.ConfirmAsync("Update the app?"))
        {
            return;
        }

        AppUpdateResult result;
        using (var loading = dialog.Loading("Downloading..."))
        {
            result = await appUpdater.DownloadAsync(location);
            log.InfoAppUpdateRequested(result);
            if (result == AppUpdateResult.Downloaded)
            {
                loading.Update("Updating...");
                result = await appUpdater.InstallAsync();
                log.InfoAppUpdateRequested(result);
            }
        }

        switch (result)
        {
            case AppUpdateResult.UpToDate:
                await dialog.InformationAsync("The app is up to date");
                break;
            case AppUpdateResult.InvalidPackage:
                await dialog.InformationAsync("The package is not this app");
                break;
            case AppUpdateResult.InstallFailed:
                await dialog.InformationAsync("Update failed");
                break;
            default:
                await dialog.InformationAsync("Download failed");
                break;
        }
    }
}
