namespace Template.MobileApp.Modules.Main;

public sealed class MenuViewModel : AppViewModelBase
{
    protected override Task OnNotifyBackAsync()
    {
        AndroidHelper.MoveTaskToBack();
        return Task.CompletedTask;
    }
}
