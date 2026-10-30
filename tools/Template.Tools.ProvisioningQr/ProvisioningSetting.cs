namespace Template.Tools.ProvisioningQr;

internal sealed class ProvisioningSetting
{
    public string Component { get; set; } = string.Empty;

    public string DownloadLocation { get; set; } = string.Empty;

    public string SignatureSha256 { get; set; } = string.Empty;

    public bool LeaveAllSystemAppsEnabled { get; set; }

    public bool AllowOffline { get; set; }

    public string? WifiSsid { get; set; }

    public string WifiSecurityType { get; set; } = "NONE";

    public string? WifiPassword { get; set; }

    public Dictionary<string, string> AdminExtras { get; } = [];
}
