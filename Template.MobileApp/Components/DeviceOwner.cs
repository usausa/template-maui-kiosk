namespace Template.MobileApp.Components;

public sealed partial class DeviceOwner : IDisposable
{
    public const string ApplyPolicyKey = "ApplyPolicy";

    public bool IsDeviceOwner => ResolveDeviceOwner();

    public bool IsPolicyApplied => ResolvePolicyApplied();

    public SystemUpdateWindow? AppliedSystemUpdateWindow => ResolveAppliedSystemUpdateWindow();

    // ------------------------------------------------------------
    // Lock task
    // ------------------------------------------------------------

    public void AllowLockTask(IReadOnlyList<string> lockTaskPackages) => PlatformAllowLockTask(lockTaskPackages);

    public void DisallowLockTask() => PlatformDisallowLockTask();

    // ------------------------------------------------------------
    // Policy
    // ------------------------------------------------------------

    public void ApplyPolicy(SystemUpdateWindow systemUpdateWindow, IReadOnlyList<string> lockTaskPackages) =>
        PlatformApplyPolicy(systemUpdateWindow, lockTaskPackages);

    public void ClearPolicy() => PlatformClearPolicy();

    public void Reboot() => PlatformReboot();

    // ------------------------------------------------------------
    // Configuration
    // ------------------------------------------------------------

    public void SendConfiguration(ManagedSettings settings)
    {
        PlatformSendConfiguration(settings);
        if (IsPolicyApplied)
        {
            PlatformSetSystemUpdateWindow(GetSystemUpdateWindow(settings));
        }

        if (ResolveLockTaskAllowed())
        {
            PlatformAllowLockTask(ManagedConfiguration.ParseLockTaskPackages(settings.LockTaskPackages));
        }
    }

    public void ClearConfiguration() => SendConfiguration(new ManagedSettings(null, null, null, null, null, null));

    // ------------------------------------------------------------
    // Provisioning
    // ------------------------------------------------------------

    public void CompleteProvisioning(ManagedSettings settings, bool applyPolicy)
    {
        SendConfiguration(settings);
        if (applyPolicy && !IsPolicyApplied)
        {
            ApplyPolicy(GetSystemUpdateWindow(settings), ManagedConfiguration.ParseLockTaskPackages(settings.LockTaskPackages));
        }
    }

    private static SystemUpdateWindow GetSystemUpdateWindow(ManagedSettings settings) =>
        SystemUpdateWindow.TryParse(settings.SystemUpdateWindow, out var window) ? window : SystemUpdateWindow.Default;

    private partial bool ResolveDeviceOwner();

    private partial bool ResolvePolicyApplied();

    private partial SystemUpdateWindow? ResolveAppliedSystemUpdateWindow();

    private partial bool ResolveLockTaskAllowed();

    private partial void PlatformAllowLockTask(IReadOnlyList<string> lockTaskPackages);

    private partial void PlatformDisallowLockTask();

    private partial void PlatformApplyPolicy(SystemUpdateWindow systemUpdateWindow, IReadOnlyList<string> lockTaskPackages);

    private partial void PlatformClearPolicy();

    private partial void PlatformReboot();

    private partial void PlatformSetSystemUpdateWindow(SystemUpdateWindow systemUpdateWindow);

    private partial void PlatformSendConfiguration(ManagedSettings settings);
}
