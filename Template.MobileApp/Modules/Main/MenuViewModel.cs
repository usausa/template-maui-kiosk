namespace Template.MobileApp.Modules.Main;

public sealed class MenuViewModel : AppViewModelBase
{
    // Command

    public IObserveCommand ForwardCommand { get; }

    public MenuViewModel()
    {
        ForwardCommand = MakeAsyncCommand<ViewId>(x => Navigator.ForwardAsync(x));
    }

    protected override Task OnNotifyBackAsync()
    {
        AndroidHelper.MoveTaskToBack();
        return Task.CompletedTask;
    }
}
