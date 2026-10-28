namespace Template.MobileApp.Modules.Main;

public sealed class MenuViewModel : AppViewModelBase
{
    // Field

    private readonly IDialog dialog;

    private readonly IPopupNavigator popupNavigator;

    // Property

    public KioskState Kiosk { get; }

    // Command

    public IObserveCommand ForwardCommand { get; }

    public IObserveCommand AdminCommand { get; }

    //--------------------------------------------------------------------------------
    // Constructor
    //--------------------------------------------------------------------------------

    public MenuViewModel(
        IDialog dialog,
        IPopupNavigator popupNavigator,
        KioskState kiosk)
    {
        this.dialog = dialog;
        this.popupNavigator = popupNavigator;
        Kiosk = kiosk;

        ForwardCommand = MakeAsyncCommand<ViewId>(x => Navigator.ForwardAsync(x));
        AdminCommand = MakeAsyncCommand(OpenAdminAsync);
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    protected override Task OnNotifyBackAsync()
    {
        AndroidHelper.MoveTaskToBack();
        return Task.CompletedTask;
    }

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task OpenAdminAsync()
    {
        var pin = await popupNavigator.InputPinAsync("Admin PIN", Length.AdminPin);
        if (pin is null)
        {
            return;
        }

        if (pin != Kiosk.AdminPin)
        {
            await dialog.InformationAsync("PIN is incorrect");
            return;
        }

        await Navigator.ForwardAsync(ViewId.KioskAdmin);
    }
}
