namespace Template.MobileApp.Components;

public sealed partial class DeviceOwner : IDisposable
{
    public bool IsDeviceOwner => ResolveDeviceOwner();

    // ------------------------------------------------------------
    // Configuration
    // ------------------------------------------------------------

    public void SendConfiguration(ManagedSettings settings) => PlatformSendConfiguration(settings);

    public void ClearConfiguration() => PlatformSendConfiguration(new ManagedSettings(null, null, null, null));

    private partial bool ResolveDeviceOwner();

    private partial void PlatformSendConfiguration(ManagedSettings settings);
}
