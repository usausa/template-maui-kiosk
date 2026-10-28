namespace Template.MobileApp.Components;

public sealed partial class DeviceOwner : IDisposable
{
    public bool IsDeviceOwner => ResolveDeviceOwner();

    // ------------------------------------------------------------
    // Lock task
    // ------------------------------------------------------------

    public void AllowLockTask(bool allow) => PlatformAllowLockTask(allow);

    // ------------------------------------------------------------
    // Configuration
    // ------------------------------------------------------------

    public void SendConfiguration(ManagedSettings settings) => PlatformSendConfiguration(settings);

    public void ClearConfiguration() => PlatformSendConfiguration(new ManagedSettings(null, null, null, null));

    private partial bool ResolveDeviceOwner();

    private partial void PlatformAllowLockTask(bool allow);

    private partial void PlatformSendConfiguration(ManagedSettings settings);
}
