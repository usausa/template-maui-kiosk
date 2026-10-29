namespace Template.MobileApp;

internal static partial class Log
{
    // Startup

    [LoggerMessage(Level = LogLevel.Information, Message = "Application start. version=[{version}], runtime=[{runtime}]")]
    public static partial void InfoApplicationStart(this ILogger logger, Version? version, Version runtime);

    // State

    [LoggerMessage(Level = LogLevel.Debug, Message = "Screen state changed. state=[{on}]")]
    public static partial void DebugScreenStateChanged(this ILogger logger, bool on);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Battery info changed. level=[{chargeLevel}], state=[{state}], source=[{powerSource}]")]
    public static partial void DebugBatteryState(this ILogger logger, double chargeLevel, BatteryState state, BatteryPowerSource powerSource);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Connectivity changed. profile=[{profile}], access=[{access}]")]
    public static partial void DebugConnectivityState(this ILogger logger, NetworkProfile profile, NetworkAccess access);

    // Kiosk

    [LoggerMessage(Level = LogLevel.Debug, Message = "Managed configuration changed. adminPinConfigured=[{adminPinConfigured}], keepScreenOn=[{keepScreenOn}], hideSystemBars=[{hideSystemBars}], message=[{message}]")]
    public static partial void DebugManagedConfiguration(this ILogger logger, bool adminPinConfigured, bool? keepScreenOn, bool? hideSystemBars, string? message);

    [LoggerMessage(Level = LogLevel.Information, Message = "Lock task state changed. state=[{state}], permitted=[{permitted}]")]
    public static partial void InfoLockTaskStateChanged(this ILogger logger, Components.LockTaskState state, bool permitted);

    [LoggerMessage(Level = LogLevel.Information, Message = "Device policy state changed. applied=[{applied}]")]
    public static partial void InfoDevicePolicyStateChanged(this ILogger logger, bool applied);

    [LoggerMessage(Level = LogLevel.Information, Message = "Reboot requested.")]
    public static partial void InfoRebootRequested(this ILogger logger);

    // Navigation

    [LoggerMessage(Level = LogLevel.Warning, Message = "Unhandled navigation error.")]
    public static partial void WarnUnhandledNavigationError(this ILogger logger, Exception exception);

#if DEBUG
    [LoggerMessage(Level = LogLevel.Warning, Message = "Leak suspected. target=[{target}]")]
    public static partial void WarnLeakSuspected(this ILogger logger, string target);

    [LoggerMessage(Level = LogLevel.Debug, Message = "Closed object collected. target=[{target}]")]
    public static partial void DebugClosedObjectCollected(this ILogger logger, string target);
#endif
}
