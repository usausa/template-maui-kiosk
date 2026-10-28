namespace Template.MobileApp.Components;

public sealed partial class DeviceOwner : IDisposable
{
    public bool IsDeviceOwner => ResolveDeviceOwner();

    public bool IsPolicyApplied => ResolvePolicyApplied();

    // ------------------------------------------------------------
    // Lock task
    // ------------------------------------------------------------

    public void AllowLockTask(bool allow) => PlatformAllowLockTask(allow);

    // ------------------------------------------------------------
    // Policy
    // ------------------------------------------------------------

    public void ApplyPolicy() => PlatformApplyPolicy();

    public void ClearPolicy() => PlatformClearPolicy();

    public void Reboot() => PlatformReboot();

    // ------------------------------------------------------------
    // Configuration
    // ------------------------------------------------------------

    public void SendConfiguration(ManagedSettings settings) => PlatformSendConfiguration(settings);

    public void ClearConfiguration() => PlatformSendConfiguration(new ManagedSettings(null, null, null, null));

    private partial bool ResolveDeviceOwner();

    private partial bool ResolvePolicyApplied();

    private partial void PlatformAllowLockTask(bool allow);

    private partial void PlatformApplyPolicy();

    private partial void PlatformClearPolicy();

    private partial void PlatformReboot();

    private partial void PlatformSendConfiguration(ManagedSettings settings);
}
