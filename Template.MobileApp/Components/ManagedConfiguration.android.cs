namespace Template.MobileApp.Components;

using Android.App;
using Android.Content;

using AndroidX.Core.Content;

public sealed partial class ManagedConfiguration
{
    private readonly RestrictionsManager restrictionsManager =
        (RestrictionsManager)Application.Context.GetSystemService(Context.RestrictionsService)!;

    private readonly ChangedReceiver receiver;

    public ManagedConfiguration()
    {
        receiver = new ChangedReceiver(this);
    }

    public void Dispose()
    {
        Stop();

        receiver.Dispose();
        restrictionsManager.Dispose();
    }

    private partial ManagedSettings ReadSettings()
    {
        using var bundle = restrictionsManager.ApplicationRestrictions;
        if (bundle is null)
        {
            return Empty;
        }

        return new ManagedSettings(
            bundle.ContainsKey(AdminPinKey) ? bundle.GetString(AdminPinKey) : null,
            bundle.ContainsKey(KeepScreenOnKey) ? bundle.GetBoolean(KeepScreenOnKey) : null,
            bundle.ContainsKey(HideSystemBarsKey) ? bundle.GetBoolean(HideSystemBarsKey) : null,
            bundle.ContainsKey(MessageKey) ? bundle.GetString(MessageKey) : null);
    }

    private partial void StartReceiver()
    {
        using var filter = new IntentFilter(Intent.ActionApplicationRestrictionsChanged);
        ContextCompat.RegisterReceiver(Application.Context, receiver, filter, ContextCompat.ReceiverNotExported)?.Dispose();
    }

    private partial void StopReceiver() => Application.Context.UnregisterReceiver(receiver);

    private sealed class ChangedReceiver : BroadcastReceiver
    {
        private readonly ManagedConfiguration owner;

        public ChangedReceiver(ManagedConfiguration owner)
        {
            this.owner = owner;
        }

        public override void OnReceive(Context? context, Intent? intent) => owner.Reload();
    }
}
