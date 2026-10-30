# Template project for MAUI kiosk

English | [日本語](README.ja.md)

A MAUI template for Android kiosk apps on dedicated devices.  
It is used from an MDM, or works as a simple MDM with the app as the device owner.

## ✨ Features

- Managed configurations
- Lock task mode
- Other apps during lock task mode
- Admin screen with a PIN
- Full screen during lock task mode
- Device policy as the device owner
- System update window
- Home app
- Reboot
- App feedback to the MDM
- Device owner setup with a QR code

## 📱 Screens

| Screen | Source | Contents |
|---|---|---|
| Menu | `Modules/Main/MenuView.xaml` | Configuration message, and the Status, Apps, and Admin buttons |
| Status | `Modules/Kiosk/KioskStatusView.xaml` | Device state, configuration values, app feedback, and version |
| Apps | `Modules/Kiosk/KioskAppsView.xaml` | Buttons that open the allowed apps |
| Admin | `Modules/Kiosk/KioskAdminView.xaml` | Lock task, device policy, reboot, and test configuration |

## 🧩 Android technologies

The state is gathered in `State/KioskState.cs`.  
Kind: 🧱 copy as is / 📝 change for the app

### 📋 Managed configurations

The app reads the values set by the MDM.  
Keys that are not set are not returned, so the app holds the default values.  
Reference: [Managed configurations](https://developer.android.com/work/managed-configurations)

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Platforms/Android/Resources/xml/app_restrictions.xml` | Declaration of the items |
| 🧱 | `Platforms/Android/MainApplication.cs` | Registration of the declaration (meta-data) |
| 📝 | `Components/ManagedConfiguration.cs` | Keys and reload |
| 📝 | `Components/ManagedConfiguration.android.cs` | Reading and receiving changes |
| 📝 | `State/KioskState.cs` | Default values |
| 📝 | `MainPageViewModel.cs` | Reload when the app returns to the front (`OnResumed`) |

| Key | Type | Default | Use |
|---|---|---|---|
| `AdminPin` | string | `0000` | PIN of the admin screen |
| `KeepScreenOn` | bool | `true` | Keep the screen on during lock task mode |
| `HideSystemBars` | bool | `true` | Hide the system bars during lock task mode |
| `Message` | string | | Message on the menu |
| `SystemUpdateWindow` | string | `02:00-04:00` | Window for automatic system updates |
| `LockTaskPackages` | string | | Other apps in lock task mode (comma-separated) |

```xml
<restriction android:key="AdminPin" android:title="Admin PIN" android:restrictionType="string" android:defaultValue="0000" />
```

```csharp
[MetaData("android.content.APP_RESTRICTIONS", Resource = "@xml/app_restrictions")]
public sealed class MainApplication : MauiApplication

using var bundle = restrictionsManager.ApplicationRestrictions;
var adminPin = bundle.ContainsKey(AdminPinKey) ? bundle.GetString(AdminPinKey) : null;
```

### 👑 Device owner

The app has a device admin receiver, and becomes the device owner with `dpm set-device-owner`.  
The Debug build is test-only, so `dpm remove-active-admin` can remove it.

| Kind | Source | Contents |
|---|---|---|
| 🧱 | `Platforms/Android/KioskAdminReceiver.cs` | Device admin receiver |
| 🧱 | `Platforms/Android/Resources/xml/device_admin.xml` | Policies used (none) |
| 🧱 | `Platforms/Android/AndroidManifest.xml` | `android:testOnly` |
| 🧱 | `Template.MobileApp.csproj` | Test-only for Debug only |
| 📝 | `Components/DeviceOwner.cs` | Public operations |
| 📝 | `Components/DeviceOwner.android.cs` | Device owner check |

```csharp
[BroadcastReceiver(Name = "template.mobileapp.KioskAdminReceiver", Exported = true, Permission = "android.permission.BIND_DEVICE_ADMIN")]
[MetaData("android.app.device_admin", Resource = "@xml/device_admin")]
[IntentFilter(["android.app.action.DEVICE_ADMIN_ENABLED"])]
public sealed class KioskAdminReceiver : DeviceAdminReceiver
```

```xml
<PropertyGroup Condition="'$(Configuration)' == 'Debug'">
  <AndroidManifestPlaceholders>testOnly=true</AndroidManifestPlaceholders>
</PropertyGroup>
```

### 📷 Setup with a QR code

On a new or reset device, tapping the welcome screen six times opens the QR code reader, and the device installs the app as the device owner.  
The app answers the fully managed mode (`GET_PROVISIONING_MODE`), and applies the admin extras at the end of the setup (`ADMIN_POLICY_COMPLIANCE`).  
The admin extras take the configuration keys, and `ApplyPolicy` (`true` applies the device policy too).  
`PROVISIONING_ALLOW_OFFLINE` continues the setup when the update of the device management app fails.  
Reference: [Provision devices](https://developers.google.com/android/work/play/emm-api/prov-devices#qr_code_method)

| Kind | Source | Contents |
|---|---|---|
| 🧱 | `Platforms/Android/ProvisioningModeActivity.cs` | Fully managed mode (`GET_PROVISIONING_MODE`) |
| 🧱 | `Platforms/Android/PolicyComplianceActivity.cs` | Admin extras (`ADMIN_POLICY_COMPLIANCE`) |
| 📝 | `Components/DeviceOwner.cs` | Configuration and policy (`CompleteProvisioning`) |
| 📝 | `tools/Template.Tools.ProvisioningQr/appsettings.json` | Contents of the QR code |
| 🧱 | `tools/Template.Tools.ProvisioningQr/Program.cs` | JSON and PNG of the QR code |

```json
{
  "android.app.extra.PROVISIONING_DEVICE_ADMIN_COMPONENT_NAME": "template.kioskapp/template.mobileapp.KioskAdminReceiver",
  "android.app.extra.PROVISIONING_DEVICE_ADMIN_PACKAGE_DOWNLOAD_LOCATION": "https://example.com/template.kioskapp-Signed.apk",
  "android.app.extra.PROVISIONING_DEVICE_ADMIN_SIGNATURE_CHECKSUM": "0R5w9t-qEBPSxPSixummCJ0hoc3yB2ve4-5NWb7E7-s",
  "android.app.extra.PROVISIONING_LEAVE_ALL_SYSTEM_APPS_ENABLED": true,
  "android.app.extra.PROVISIONING_ALLOW_OFFLINE": true,
  "android.app.extra.PROVISIONING_ADMIN_EXTRAS_BUNDLE": { "AdminPin": "1234", "ApplyPolicy": "true" }
}
```

### 🔒 Lock task mode

An allowed app enters lock task mode at launch, and HOME and recent apps do not leave it.  
Without the permission, it is screen pinning (the user confirms it, and can stop it).  
Reference: [Lock task mode](https://developer.android.com/work/dpc/dedicated-devices/lock-task-mode)

| Kind | Source | Contents |
|---|---|---|
| 🧱 | `Platforms/Android/MainActivity.cs` | `LockTaskMode = "if_whitelisted"`, `SingleTask` |
| 🧱 | `Platforms/Android/KioskAdminReceiver.cs` | Enter and exit notices |
| 🧱 | `Components/LockTask.cs` | State watch, waiting for the stop (`StopAsync`) |
| 🧱 | `Components/LockTask.android.cs` | `startLockTask()`, `stopLockTask()`, state, permission |
| 📝 | `Components/DeviceOwner.android.cs` | Allowing itself and other apps (`setLockTaskPackages()`) |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | Start, Stop |

```csharp
[Activity(LaunchMode = LaunchMode.SingleTask, LockTaskMode = "if_whitelisted", ...)]
public sealed class MainActivity : MauiAppCompatActivity

policyManager.SetLockTaskPackages(admin, [Application.Context.PackageName!]);
ActivityResolver.CurrentActivity.StartLockTask();
```

### 📦 Other apps

The apps in the `LockTaskPackages` configuration are allowed during lock task mode, as well as this app.  
Apps on the menu opens them, and Back returns to the app.  
Reading the labels of other apps needs `<queries>` for launcher apps.  
Reference: [Package visibility filtering on Android](https://developer.android.com/training/package-visibility)

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | Allowing the apps (`setLockTaskPackages()`) |
| 📝 | `Modules/Kiosk/KioskAppsView.xaml` | Apps screen |
| 📝 | `Modules/Kiosk/KioskAppsViewModel.cs` | Allowed apps and launch |
| 🧱 | `Platforms/Android/AndroidHelper.cs` | Label and launch (`FindAppLabel`, `LaunchApp`) |
| 🧱 | `Platforms/Android/AndroidManifest.xml` | `<queries>` for launcher apps |

```csharp
policyManager.SetLockTaskPackages(admin, [Application.Context.PackageName!, .. lockTaskPackages]);

using var intent = packageManager.GetLaunchIntentForPackage(packageName);
intent.AddFlags(ActivityFlags.NewTask);
Application.Context.StartActivity(intent);
```

### 📺 Full screen and keep screen on

During lock task mode, the system bars are hidden and the screen stays on, by the configurations.  
A swipe shows the system bars for a while.

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Platforms/Android/MainActivity.cs` | Window update (`UpdateKioskWindow`) |

```csharp
window.AddFlags(WindowManagerFlags.KeepScreenOn);
controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
controller.Hide(WindowInsetsCompat.Type.SystemBars());
```

### 🔑 Admin screen

Admin on the menu opens the admin screen after the PIN.  
The admin screen stops lock task mode, and opens the Android settings.

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Modules/Main/MenuViewModel.cs` | PIN check |
| 🧱 | `Modules/PopupNavigatorExtensions.cs` | PIN input (`InputPinAsync`) |
| 🧱 | `Modules/Navigation/Modal/InputNumberView.xaml` | Number input popup |
| 🧱 | `Modules/Navigation/Modal/InputNumberViewModel.cs` | Masked digits, leading zeros kept |
| 🧱 | `Models/Input/` | Input value |
| 📝 | `Modules/Kiosk/KioskAdminView.xaml` | Admin screen |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | Admin screen operations |
| 🧱 | `Platforms/Android/AndroidHelper.cs` | Opening the settings (`OpenSettings`) |

```csharp
var pin = await popupNavigator.InputPinAsync("Admin PIN", Length.AdminPin);
if (pin == Kiosk.AdminPin)
{
    await Navigator.ForwardAsync(ViewId.KioskAdmin);
}
```

### 🏠 Home app

`HomeActivity` only receives HOME and opens `MainActivity`, and is disabled by default.  
Apply enables it and fixes it as the home app, so the app starts after a reboot.  
`MainActivity` is not the home itself, since HOME would start a second instance in another task and the app would always be a home candidate.  
A home activity disabled by default, then enabled and fixed, is the same pattern as `KioskModeActivity` in [Test DPC](https://github.com/googlesamples/android-testdpc).

| Kind | Source | Contents |
|---|---|---|
| 🧱 | `Platforms/Android/HomeActivity.cs` | HOME relay (disabled by default) |
| 📝 | `Components/DeviceOwner.android.cs` | Enabling it and fixing it as the home app |

```csharp
[Activity(Theme = "@android:style/Theme.NoDisplay", TaskAffinity = "", Enabled = false, Exported = true)]
[IntentFilter([Intent.ActionMain], Categories = [Intent.CategoryHome, Intent.CategoryDefault])]
public sealed class HomeActivity : Activity

packageManager.SetComponentEnabledSetting(home, ComponentEnabledState.Enabled, ComponentEnableOption.DontKillApp);
policyManager.AddPersistentPreferredActivity(admin, filter, home);
```

### 📜 Device policy

Apply sets the following as the device owner, and Clear removes them.  
Reference: [Dedicated devices cookbook](https://developer.android.com/work/dpc/dedicated-devices/cookbook)

| Policy | API | Value |
|---|---|---|
| Lock task permission | `setLockTaskPackages()` | This app and other apps |
| System info | `setLockTaskFeatures()` | `LOCK_TASK_FEATURE_SYSTEM_INFO` |
| Power menu | `setLockTaskFeatures()` | `LOCK_TASK_FEATURE_GLOBAL_ACTIONS` |
| Home app | `addPersistentPreferredActivity()` | `HomeActivity` |
| No lock screen (without a PIN) | `setKeyguardDisabled()` | `true` |
| Screen on while charging | `setGlobalSetting()` | `STAY_ON_WHILE_PLUGGED_IN` |
| No safe boot | `addUserRestriction()` | `DISALLOW_SAFE_BOOT` |
| No factory reset | `addUserRestriction()` | `DISALLOW_FACTORY_RESET` |
| No new users | `addUserRestriction()` | `DISALLOW_ADD_USER` |
| No external media | `addUserRestriction()` | `DISALLOW_MOUNT_PHYSICAL_MEDIA` |
| System updates | `setSystemUpdatePolicy()` | Window from the configuration |

Clear waits for lock task mode to stop, and removes the permission first (another order closes the app).  
Screen on while charging goes back to the value before Apply.

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | `PlatformApplyPolicy`, `PlatformClearPolicy` |
| 🧱 | `Components/LockTask.cs` | Waiting for the stop (`StopAsync`) |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | Apply, Clear |

```csharp
policyManager.SetLockTaskFeatures(admin, LockTaskFeatures.SystemInfo | LockTaskFeatures.GlobalActions);
policyManager.SetKeyguardDisabled(admin, true);
policyManager.AddUserRestriction(admin, UserManager.DisallowFactoryReset);
```

### 🕑 System update window

Apply limits automatic system updates to a daily window from the `SystemUpdateWindow` configuration (`HH:mm-HH:mm`, `02:00-04:00` by default).  
A configuration change while the policy is applied sets the new window, and Clear removes it.  
The window is in minutes from midnight (`120` to `240`), and a start after the end spans midnight.  
Reference: [Manage system updates](https://developer.android.com/work/dpc/system-updates)

| Kind | Source | Contents |
|---|---|---|
| 🧱 | `Components/SystemUpdateWindow.cs` | Window (`HH:mm-HH:mm`) |
| 📝 | `Components/DeviceOwner.cs` | Setting it again on a configuration change |
| 📝 | `Components/DeviceOwner.android.cs` | Setting and reading the policy |
| 📝 | `Modules/Kiosk/KioskStatusView.xaml` | Current policy (System update) |

```csharp
using var policy = SystemUpdatePolicy.CreateWindowedInstallPolicy(120, 240);
policyManager.SetSystemUpdatePolicy(admin, policy);
```

### 🔌 Reboot

Reboot restarts the device after a confirmation.

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | Reboot (`reboot()`) |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | Confirmation |

```csharp
if (await dialog.ConfirmAsync("Reboot the device?"))
{
    deviceOwner.Reboot();
}
```

### 🧪 Test configuration

The device owner sets the configurations of its own app, to try them without an MDM.

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | Setting (`setApplicationRestrictions()`) |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | Send, Clear |

```csharp
bundle.PutString(ManagedConfiguration.AdminPinKey, settings.AdminPin);
policyManager.SetApplicationRestrictions(admin, Application.Context.PackageName, bundle);
```

### 📨 App feedback

The app reports its states to the MDM as keyed app states, at launch and when they change.  
As a simple MDM, the app also receives them as the device owner, and Status shows the last ones.  
Reference: [Send app feedback to EMMs](https://developer.android.com/work/app-feedback/overview)

| Key | Severity | Contents |
|---|---|---|
| `LockTask` | Info | `None`, `Locked`, or `Pinned` |
| `DevicePolicy` | Info | `Applied` or `NotApplied` |
| `AdminPin` | Info / Error | Error when the PIN is not digits |
| `SystemUpdateWindow` | Info / Error | Error when it is not `HH:mm-HH:mm` |

| Kind | Source | Contents |
|---|---|---|
| 🧱 | `Template.MobileApp.csproj` | `Xamarin.AndroidX.Enterprise.Feedback` |
| 🧱 | `Components/AppFeedback.cs` | States, report, and receive |
| 🧱 | `Components/AppFeedback.android.cs` | Report (`KeyedAppStatesReporter`) |
| 🧱 | `Platforms/Android/KioskAppStatesService.cs` | Receiving (`KeyedAppStatesService`) |
| 📝 | `State/KioskState.cs` | States to report |

```csharp
reporter.SetStates(keyedStates, null);

[Service(Name = "template.mobileapp.KioskAppStatesService", Exported = true)]
[IntentFilter(["androidx.enterprise.feedback.action.APP_STATES"])]
public sealed class KioskAppStatesService : KeyedAppStatesService
```

### 💾 Backup

The app data is not backed up or transferred.

| Kind | Source | Contents |
|---|---|---|
| 🧱 | `Platforms/Android/AndroidManifest.xml` | `allowBackup="false"`, `dataExtractionRules` |
| 🧱 | `Platforms/Android/Resources/xml/data_extraction_rules.xml` | All domains excluded |

```xml
<application android:allowBackup="false" android:dataExtractionRules="@xml/data_extraction_rules" ... />
```

## 🔄 How it works

### Used from an MDM

1. The MDM installs the app, allows lock task mode, and sets the configurations
2. The app enters lock task mode in full screen at launch
3. Configuration changes are applied right away
4. The app reports its states to the MDM

### As a simple MDM

1. Set the app as the device owner with a QR code (or `dpm` on a test device)
2. Apply on the admin screen sets the device policy, and enters lock task mode in full screen
3. After a reboot, the app starts in lock task mode
4. Apps on the menu opens the allowed apps

### Maintenance

1. Admin on the menu asks for the PIN (`0000` by default)
2. Stop ends lock task mode, and Open settings opens the settings
3. Start returns to lock task mode, and Clear removes the device policy

## 🔧 Setup

### Device owner

Install the Debug build on a device without accounts (such as an emulator), and run the following.

```
adb shell dpm set-device-owner template.kioskapp/template.mobileapp.KioskAdminReceiver
```

Open Admin on the menu with the PIN `0000`, and press Apply.

### QR code

Set the APK URL, the SHA-256 of the signing certificate (`keytool -list -v`), and the admin extras in `tools/Template.Tools.ProvisioningQr/appsettings.json`, and run the tool.  
The tool writes `provisioning.json` and `provisioning.png`, and the values can also be given on the command line.  
Use the signed Release build.

```
dotnet run --project tools/Template.Tools.ProvisioningQr -- --Provisioning:DownloadLocation=https://example.com/app.apk
```

On a new or reset device, tap the welcome screen six times, and read the QR code.  
The Release build cannot be removed with `dpm remove-active-admin`, so reset the device to set it up again.

### Removing the device owner

Press Clear on the admin screen first (removing it with the policy applied leaves the app as the home app).

```
adb shell dpm remove-active-admin template.kioskapp/template.mobileapp.KioskAdminReceiver
```

### MDM

Register the app in the MDM, allow lock task mode, and set the configuration keys.

## 🔗 Links

- [template-maui](https://github.com/usausa/template-maui)
