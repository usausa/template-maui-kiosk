using System.Buffers.Text;
using System.Text.Json.Nodes;

using Microsoft.Extensions.Configuration;

using QRCoder;

using Template.Tools.ProvisioningQr;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .AddCommandLine(args)
    .Build();
var setting = configuration.GetSection("Provisioning").Get<ProvisioningSetting>() ?? new ProvisioningSetting();
var output = configuration["Output"] ?? "provisioning";

var json = new JsonObject
{
    ["android.app.extra.PROVISIONING_DEVICE_ADMIN_COMPONENT_NAME"] = setting.Component,
    ["android.app.extra.PROVISIONING_DEVICE_ADMIN_PACKAGE_DOWNLOAD_LOCATION"] = setting.DownloadLocation,
    ["android.app.extra.PROVISIONING_DEVICE_ADMIN_SIGNATURE_CHECKSUM"] = Base64Url.EncodeToString(Convert.FromHexString(setting.SignatureSha256.Replace(":", string.Empty, StringComparison.Ordinal))),
    ["android.app.extra.PROVISIONING_LEAVE_ALL_SYSTEM_APPS_ENABLED"] = setting.LeaveAllSystemAppsEnabled,
    ["android.app.extra.PROVISIONING_ALLOW_OFFLINE"] = setting.AllowOffline
};

if (!String.IsNullOrEmpty(setting.WifiSsid))
{
    json["android.app.extra.PROVISIONING_WIFI_SSID"] = setting.WifiSsid;
    json["android.app.extra.PROVISIONING_WIFI_SECURITY_TYPE"] = setting.WifiSecurityType;
    if (!String.IsNullOrEmpty(setting.WifiPassword))
    {
        json["android.app.extra.PROVISIONING_WIFI_PASSWORD"] = setting.WifiPassword;
    }
}

var extras = new JsonObject();
foreach (var (key, value) in setting.AdminExtras)
{
    if (!String.IsNullOrEmpty(value))
    {
        extras[key] = value;
    }
}

if (extras.Count > 0)
{
    json["android.app.extra.PROVISIONING_ADMIN_EXTRAS_BUNDLE"] = extras;
}

var text = json.ToJsonString();
File.WriteAllText($"{output}.json", text);

using var generator = new QRCodeGenerator();
using var data = generator.CreateQrCode(text, QRCodeGenerator.ECCLevel.M);
using var png = new PngByteQRCode(data);
File.WriteAllBytes($"{output}.png", png.GetGraphic(10));

Console.WriteLine(text);
