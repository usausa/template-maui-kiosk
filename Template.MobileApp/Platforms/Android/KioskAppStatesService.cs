#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace Template.MobileApp;

using Android.App;

using AndroidX.Enterprise.Feedback;

using Template.MobileApp.Components;

[Service(
    Name = "template.mobileapp.KioskAppStatesService",
    Exported = true)]
[IntentFilter(["androidx.enterprise.feedback.action.APP_STATES"])]
public sealed class KioskAppStatesService : KeyedAppStatesService
{
    public override void OnReceive(ICollection<ReceivedKeyedAppState> states, bool requestSync)
    {
        var received = states
            .Select(static x => new AppState(
                x.Key,
                x.Severity == KeyedAppState.SeverityError ? AppStateSeverity.Error : AppStateSeverity.Info,
                x.Message,
                x.Data))
            .OrderBy(static x => x.Key, StringComparer.Ordinal)
            .ToList();

        IPlatformApplication.Current?.Services.GetService<AppFeedback>()?.Receive(received);
    }
}
