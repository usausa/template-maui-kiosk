# Template project for MAUI kiosk

English | [日本語](README.ja.md)

A MAUI template for Android kiosk apps on dedicated devices.  
It is used from an MDM, or works as a simple MDM with the app as the device owner.

## ✨ Features

- Managed configurations
- Lock task mode
- Other apps during lock task mode
- Admin screens with a PIN
- Full screen during lock task mode
- Device policy as the device owner
- System update window
- Home app
- Reboot
- App feedback to the MDM
- Device owner setup with a QR code
- App update by itself
- Disabled status bar and hidden apps
- Automatic permission grants
- Lock screen and support messages
- Wait after wrong PINs

## 📱 Screens

| Screen | Source | Contents |
|---|---|---|
| Menu | `Modules/Main/MenuView.xaml` | Configuration message, the screen buttons, and Reboot |
| Status | `Modules/Kiosk/KioskStatusView.xaml` | Device state, configuration values, app feedback, and version |
| Apps | `Modules/Kiosk/KioskAppsView.xaml` | Buttons that open the allowed apps |
| Lock task | `Modules/Kiosk/KioskLockTaskView.xaml` | Lock task state, Start, Stop, and Open settings |
| Device policy | `Modules/Kiosk/KioskPolicyView.xaml` | Device policy state, Apply, and Clear |
| App update | `Modules/Kiosk/KioskUpdateView.xaml` | Version, and Update |
| Test configuration | `Modules/Kiosk/KioskConfigurationView.xaml` | Configuration values, Send, and Clear |

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
| `AdminPin` | string | `0000` | PIN of the admin screens |
| `KeepScreenOn` | bool | `true` | Keep the screen on during lock task mode |
| `HideSystemBars` | bool | `true` | Hide the system bars during lock task mode |
| `Message` | string | | Message on the menu |
| `SystemUpdateWindow` | string | `02:00-04:00` | Window for automatic system updates |
| `LockTaskPackages` | string | | Other apps in lock task mode (comma-separated) |
| `UpdateLocation` | string | | URL of the APK for the app update |
| `LockScreenInfo` | string | | Message on the lock screen |
| `SupportMessage` | string | | Message on actions blocked by the policy |

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
| 📝 | `Modules/Kiosk/KioskLockTaskViewModel.cs` | Start, Stop |

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

### 🔑 Admin screens

Lock task, Device policy, App update, and Test configuration on the menu open after the PIN, and Reboot also asks for it.  
Lock task stops lock task mode, and opens the Android settings.  
Without the device owner, the buttons other than Lock task cannot be pressed.  
Five wrong PINs in a row make it wait 30 seconds, and the count stays after the app restarts.

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Modules/Main/MenuView.xaml` | Buttons of the admin screens |
| 📝 | `Modules/Main/MenuViewModel.cs` | PIN check, and the wait after wrong PINs |
| 🧱 | `Modules/PopupNavigatorExtensions.cs` | PIN input (`InputPinAsync`) |
| 🧱 | `Modules/Navigation/Modal/InputNumberView.xaml` | Number input popup |
| 🧱 | `Modules/Navigation/Modal/InputNumberViewModel.cs` | Masked digits, leading zeros kept |
| 🧱 | `Models/Input/` | Input value |
| 🧱 | `Platforms/Android/AndroidHelper.cs` | Opening the settings (`OpenSettings`) |

```xml
<Button Command="{Binding AdminCommand}" CommandParameter="{markup:ViewId KioskPolicy}" IsEnabled="{Binding Kiosk.IsDeviceOwner}" Text="Device policy" />
```

```csharp
var pin = await popupNavigator.InputPinAsync("Admin PIN", Length.AdminPin);
if (pin == Kiosk.AdminPin)
{
    await Navigator.ForwardAsync(id);
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
| Status bar disabled | `setStatusBarDisabled()` | `true` |
| Hidden apps | `setApplicationHidden()` | Launcher and home apps except this app, other apps, and the settings |
| Automatic permission grants | `setPermissionPolicy()` | `PERMISSION_POLICY_AUTO_GRANT` |

Clear waits for lock task mode to stop, and removes the permission first (another order closes the app).  
Screen on while charging goes back to the value before Apply.  
The disabled status bar works out of lock task mode (maintenance and the app update), where the notifications and quick settings do not open.  
Hidden apps keep their data, and Clear shows them again (a configuration change while the policy is applied hides them again).  
Permissions that apps ask for are granted without a dialog, and stay granted and fixed by the admin after Clear.

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | `PlatformApplyPolicy`, `PlatformClearPolicy` |
| 📝 | `Components/DeviceOwner.cs` | Hiding the apps again on a configuration change |
| 🧱 | `Components/LockTask.cs` | Waiting for the stop (`StopAsync`) |
| 📝 | `Modules/Kiosk/KioskPolicyViewModel.cs` | Apply, Clear |
| 🧱 | `Platforms/Android/AndroidManifest.xml` | `<queries>` for home apps and the settings |

```csharp
policyManager.SetLockTaskFeatures(admin, LockTaskFeatures.SystemInfo | LockTaskFeatures.GlobalActions);
policyManager.SetKeyguardDisabled(admin, true);
policyManager.AddUserRestriction(admin, UserManager.DisallowFactoryReset);
policyManager.SetStatusBarDisabled(admin, true);
policyManager.SetApplicationHidden(admin, packageName, true);
policyManager.SetPermissionPolicy(admin, PermissionPolicy.AutoGrant);
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

### 🪪 Lock screen and support messages

The device owner shows the `LockScreenInfo` configuration on the lock screen, and the `SupportMessage` configuration on actions blocked by the policy (such as factory reset in the settings).  
They are set when the device owner sends the configuration, apart from Apply, and removed when the keys are not set.  
The lock screen does not show while the policy is applied (no lock screen), so its message shows after Clear.

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Components/DeviceOwner.cs` | Setting them with the configuration |
| 📝 | `Components/DeviceOwner.android.cs` | Setting (`PlatformSetMessages`) |
| 📝 | `Modules/Kiosk/KioskStatusView.xaml` | Configuration values |

```csharp
policyManager.SetDeviceOwnerLockScreenInfo(admin, lockScreenInfo);
policyManager.SetShortSupportMessage(admin, supportMessage);
```

### 🔌 Reboot

Reboot on the menu restarts the device after the PIN and a confirmation.

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | Reboot (`reboot()`) |
| 📝 | `Modules/Main/MenuViewModel.cs` | PIN and confirmation |

```csharp
if (await dialog.ConfirmAsync("Reboot the device?"))
{
    deviceOwner.Reboot();
}
```

### ⬆️ App update

Update on App update downloads the APK at the `UpdateLocation` configuration, and installs it when it is this app with a larger version code.  
As the device owner, the app installs it without a confirmation, and the system notifies that the admin updated the app.  
The install stops the app, and the app opens again in lock task mode on `ACTION_MY_PACKAGE_REPLACED`.  
The waiting overlay stays until the app stops, and a failed install shows Update failed.  
While the policy is applied, nothing else opens during the install, since the other home apps are hidden.  
The APK needs the same signature, and an HTTPS URL (cleartext HTTP is not allowed).  
Reference: [Install app packages](https://developer.android.com/work/dpc/dedicated-devices/cookbook#install_app_packages)

| Kind | Source | Contents |
|---|---|---|
| 🧱 | `Components/AppUpdater.cs` | Download, and waiting for the install |
| 🧱 | `Components/AppUpdater.android.cs` | Package check and install (`PackageInstaller`) |
| 🧱 | `Platforms/Android/AppUpdateReceiver.cs` | Install result, and opening the app again |
| 📝 | `Modules/Kiosk/KioskUpdateViewModel.cs` | Update |

```csharp
using var parameters = new PackageInstaller.SessionParams(PackageInstallMode.FullInstall);
parameters.SetAppPackageName(Application.Context.PackageName);
var sessionId = installer.CreateSession(parameters);
session.Commit(pendingIntent.IntentSender);

[IntentFilter([Intent.ActionMyPackageReplaced])]
public sealed class AppUpdateReceiver : BroadcastReceiver
```

### 🧪 Test configuration

The device owner sets the configurations of its own app, to try them without an MDM.

| Kind | Source | Contents |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | Setting (`setApplicationRestrictions()`) |
| 📝 | `Modules/Kiosk/KioskConfigurationViewModel.cs` | Send, Clear |

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
2. Apply on Device policy sets the device policy, and enters lock task mode in full screen
3. After a reboot, the app starts in lock task mode
4. Apps on the menu opens the allowed apps

### Maintenance

1. The admin screens on the menu ask for the PIN (`0000` by default)
2. Stop on Lock task ends lock task mode, and Open settings opens the settings
3. Start returns to lock task mode, and Clear on Device policy removes the device policy
4. Update on App update updates the app from the `UpdateLocation` configuration

## 🔧 Setup

Full screen and no switching to other apps work during lock task mode.  
The system bars are hidden and the screen stays on by default (`HideSystemBars`, `KeepScreenOn`).  
Only the apps in `LockTaskPackages` open, from Apps on the menu.  
Set it up as the device owner on a test device, with a QR code, or from an MDM.

### Device owner

Install the Debug build on a device without accounts (such as an emulator), and make the app the device owner.

```
dotnet build Template.MobileApp/Template.MobileApp.csproj -c Debug -f net10.0-android -t:Install
```

```
adb shell dpm set-device-owner template.kioskapp/template.mobileapp.KioskAdminReceiver
```

Open Device policy on the menu with the PIN `0000`, and press Apply.  
The app enters lock task mode in full screen, HOME and recent apps do not leave it, and the status bar cannot be pulled down.  
Other launchers are hidden and the app is the home app, so it starts in lock task mode after a reboot.

### QR code

Set the APK URL, the SHA-256 of the signing certificate (`keytool -list -v`), and the admin extras in `tools/Template.Tools.ProvisioningQr/appsettings.json`, and run the tool.  
`"ApplyPolicy": "true"` in the admin extras applies the device policy at the end of the setup, as Apply does.  
The tool writes `provisioning.json` and `provisioning.png`, and the values can also be given on the command line.  
Use the signed Release build.

```
dotnet run --project tools/Template.Tools.ProvisioningQr -- --Provisioning:DownloadLocation=https://example.com/app.apk
```

On a new or reset device, tap the welcome screen six times, and read the QR code.  
After the setup, the app starts as the home app in lock task mode in full screen.  
The Release build cannot be removed with `dpm remove-active-admin`, so reset the device to set it up again.

### App update

Build the signed Release build from scratch with a larger `ApplicationVersion`, put the APK on an HTTPS server, and set its URL in the `UpdateLocation` configuration.  
Check that the APK starts on a test device before putting it on the server, since an app that does not start stops the kiosk.

```
dotnet build Template.MobileApp/Template.MobileApp.csproj -c Release -f net10.0-android --no-incremental -p:ApplicationVersion=2 -p:ApplicationDisplayVersion=1.1
```

### Removing the device owner

Press Clear on Device policy first (removing it with the policy applied leaves the app as the home app).

```
adb shell dpm remove-active-admin template.kioskapp/template.mobileapp.KioskAdminReceiver
```

### MDM

Register the app in the MDM, allow lock task mode, and set the configuration keys.  
The app enters lock task mode in full screen at launch.  
The lock task settings of the MDM decide whether HOME and recent apps work.

## 📚 Docs

The documents in `docs` (Japanese) describe what each topic does, and how to check it on an emulator step by step.  
The tools for the checks on an emulator are in `tools/emulator`.

| Document | Contents |
|---|---|
| [overview.md](docs/overview.md) | What device policy is (read first) |
| [emulator.md](docs/emulator.md) | Emulator setup for the checks |
| [managed-configuration.md](docs/managed-configuration.md) | Managed configurations |
| [lock-task.md](docs/lock-task.md) | Lock task mode, full screen, and screen pinning |
| [device-policy.md](docs/device-policy.md) | Device policy, home app, and reboot |
| [status-bar-and-hidden-apps.md](docs/status-bar-and-hidden-apps.md) | Disabled status bar and hidden apps |
| [permissions.md](docs/permissions.md) | Automatic permission grants |
| [other-apps.md](docs/other-apps.md) | Other apps during lock task mode |
| [system-update.md](docs/system-update.md) | System update window |
| [app-feedback.md](docs/app-feedback.md) | App feedback to the MDM |
| [lock-screen-messages.md](docs/lock-screen-messages.md) | Lock screen and support messages |
| [admin-pin.md](docs/admin-pin.md) | Admin screens and the PIN |
| [qr-provisioning.md](docs/qr-provisioning.md) | Device owner setup with a QR code |
| [app-update.md](docs/app-update.md) | App update by itself |

## 🔗 Related Projects

| Project | Description |
| --- | --- |
| [template-maui](https://github.com/usausa/template-maui) | MAUI application template |
| [template-maui-blazor](https://github.com/usausa/template-maui-blazor) | Blazor based application template |
| [template-maui-keyboard](https://github.com/usausa/template-maui-keyboard) | Physical keyboard control example |
| [template-maui-server](https://github.com/usausa/template-maui-server) | Server application template |
