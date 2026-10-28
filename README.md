# Template project for MAUI kiosk

English | [日本語](README.ja.md)

For dedicated devices, a MAUI template project for Android kiosk apps used from an MDM, or as a simple MDM with the app itself as the device owner.

## ✨ Features

- Managed configurations
- Lock task mode
- Admin screen with a PIN
- Full screen during lock task mode
- Device policy as the device owner
- Home app
- Reboot

## 📱 Screens

| Screen | Source | Contents |
|---|---|---|
| Menu | `Modules/Main/MenuView.xaml` | Message from the managed configurations, and the Status and Admin buttons |
| Status | `Modules/Kiosk/KioskStatusView.xaml` | Device owner, lock task state, lock task permission, values of the managed configurations (MDM or default), and version |
| Admin | `Modules/Kiosk/KioskAdminView.xaml` | Lock task (Start, Stop, Open settings), Device policy (Apply, Clear, Reboot), and Test configuration (Send, Clear) |

## 🧩 Android technologies

The state used by the screens and `MainActivity` is gathered in `State/KioskState.cs`.  
The Android parts are in `Components` (`.android.cs`) and `Platforms/Android`.  
In the tables, 🧱 Base can be copied to another app as is, and 📝 App needs changes for the app.

### 📋 Managed configurations

The MDM sets values for the items declared by the app, and the app reads them.  
Only the keys set by the MDM are returned, so the app holds the default values.  
The values are read at startup and when the app returns to the front, and changes are received by a receiver registered at run time.

| Source | Kind | Contents |
|---|---|---|
| `Platforms/Android/Resources/xml/app_restrictions.xml` | 📝 App | Declaration of the items (`<restrictions>`) |
| `Platforms/Android/MainApplication.cs` | 🧱 Base | `android.content.APP_RESTRICTIONS` meta-data |
| `Components/ManagedConfiguration.cs` + `.android.cs` | 📝 App | Reading with `RestrictionsManager.getApplicationRestrictions()`, and receiving `ACTION_APPLICATION_RESTRICTIONS_CHANGED` (the keys are the app's) |
| `State/KioskState.cs` | 📝 App | Default values |
| `MainPageViewModel.cs` | 📝 App | Reading again in `OnResumed` |

| Key | Type | Default | Use |
|---|---|---|---|
| `AdminPin` | string | `0000` | PIN of the admin screen |
| `KeepScreenOn` | bool | `true` | Keep the screen on during lock task mode |
| `HideSystemBars` | bool | `true` | Hide the system bars during lock task mode |
| `Message` | string | | Message shown on the menu |

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
The Debug build is test-only, so the device owner can be removed with `dpm remove-active-admin`.

| Source | Kind | Contents |
|---|---|---|
| `Platforms/Android/KioskAdminReceiver.cs` | 🧱 Base | `DeviceAdminReceiver` |
| `Platforms/Android/Resources/xml/device_admin.xml` | 🧱 Base | Policies of the device admin (none) |
| `Components/DeviceOwner.cs` + `.android.cs` | 📝 App | Detection with `DevicePolicyManager.isDeviceOwnerApp()`, and the device policy and keys of the app |
| `Platforms/Android/AndroidManifest.xml`, `Template.MobileApp.csproj` | 🧱 Base | `android:testOnly` only for the Debug build (`AndroidManifestPlaceholders`) |

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

### 🔒 Lock task mode

An app allowed by the device owner or an MDM enters lock task mode at launch, and HOME and recent apps do not leave the app.  
Without the permission, `startLockTask()` starts screen pinning, which the user confirms and can stop.  
The state is read from `ActivityManager`, and its changes are received by the device admin receiver (polled for screen pinning).

| Source | Kind | Contents |
|---|---|---|
| `Platforms/Android/MainActivity.cs` | 🧱 Base | `LockTaskMode = "if_whitelisted"` and `SingleTask` in `[Activity]` |
| `Components/LockTask.cs` + `.android.cs` | 🧱 Base | `Activity.startLockTask()` / `stopLockTask()`, `ActivityManager.getLockTaskModeState()`, `DevicePolicyManager.isLockTaskPermitted()`, and waiting for the stop (`StopAsync`) |
| `Platforms/Android/KioskAdminReceiver.cs` | 🧱 Base | `onLockTaskModeEntering()` / `onLockTaskModeExiting()` |
| `Components/DeviceOwner.android.cs` | 📝 App | Permission with `DevicePolicyManager.setLockTaskPackages()` |
| `Modules/Kiosk/KioskAdminViewModel.cs` | 📝 App | Start and Stop |

```csharp
[Activity(LaunchMode = LaunchMode.SingleTask, LockTaskMode = "if_whitelisted", ...)]
public sealed class MainActivity : MauiAppCompatActivity

policyManager.SetLockTaskPackages(admin, [Application.Context.PackageName!]);
ActivityResolver.CurrentActivity.StartLockTask();
```

### 📺 Full screen and keep screen on

During lock task mode, the system bars are hidden and the screen stays on.  
A swipe shows the system bars for a while.  
Both follow the managed configurations.

| Source | Kind | Contents |
|---|---|---|
| `Platforms/Android/MainActivity.cs` (`UpdateKioskWindow`) | 📝 App | `WindowInsetsControllerCompat` (`BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE`) and `FLAG_KEEP_SCREEN_ON`, by the state in `KioskState` |

```csharp
window.AddFlags(WindowManagerFlags.KeepScreenOn);
controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
controller.Hide(WindowInsetsCompat.Type.SystemBars());
```

### 🔑 Admin screen

Admin on the menu opens the admin screen after the PIN of the managed configurations.  
On the admin screen, lock task mode is stopped for maintenance, and Open settings opens the Android settings.

| Source | Kind | Contents |
|---|---|---|
| `Modules/Main/MenuViewModel.cs` | 📝 App | PIN check |
| `Modules/PopupNavigatorExtensions.cs`, `Modules/Navigation/Modal/InputNumberView.xaml` + `InputNumberViewModel.cs`, `Models/Input/` | 🧱 Base | PIN input with masked digits (`InputPinAsync`) |
| `Modules/Kiosk/KioskAdminView.xaml` + `KioskAdminViewModel.cs` | 📝 App | Admin screen |
| `Platforms/Android/AndroidHelper.cs` | 🧱 Base | Android settings with `Settings.ACTION_SETTINGS` (`OpenSettings`) |

```csharp
var pin = await popupNavigator.InputPinAsync("Admin PIN", Length.AdminPin);
if (pin == Kiosk.AdminPin)
{
    await Navigator.ForwardAsync(ViewId.KioskAdmin);
}
```

### 🏠 Home app

`HomeActivity` has the HOME intent filter and is disabled by default, so it is not offered as a home app on ordinary devices.  
The device owner enables it and fixes it as the home app, so the app starts after a reboot.  
`HomeActivity` only opens `MainActivity` and closes, so `MainActivity` stays a single instance in a standard task.

| Source | Kind | Contents |
|---|---|---|
| `Platforms/Android/HomeActivity.cs` | 🧱 Base | `ACTION_MAIN` + `CATEGORY_HOME` + `CATEGORY_DEFAULT`, disabled by default, and opening `MainActivity` |
| `Components/DeviceOwner.android.cs` | 📝 App | `PackageManager.setComponentEnabledSetting()`, `DevicePolicyManager.addPersistentPreferredActivity()` / `clearPackagePersistentPreferredActivities()` |

```csharp
[Activity(Theme = "@android:style/Theme.NoDisplay", TaskAffinity = "", Enabled = false, Exported = true)]
[IntentFilter([Intent.ActionMain], Categories = [Intent.CategoryHome, Intent.CategoryDefault])]
public sealed class HomeActivity : Activity

packageManager.SetComponentEnabledSetting(home, ComponentEnabledState.Enabled, ComponentEnableOption.DontKillApp);
policyManager.AddPersistentPreferredActivity(admin, filter, home);
```

### 📜 Device policy

Apply on the admin screen sets the following as the device owner, and Clear removes them.

| Policy | Android |
|---|---|
| Lock task permission | `setLockTaskPackages()` |
| Lock task features (system info and the power menu only) | `setLockTaskFeatures()` with `LOCK_TASK_FEATURE_SYSTEM_INFO` and `LOCK_TASK_FEATURE_GLOBAL_ACTIONS` |
| Home app | `addPersistentPreferredActivity()` |
| No lock screen (only without a PIN or password) | `setKeyguardDisabled()` |
| Screen on while charging | `setGlobalSetting()` with `STAY_ON_WHILE_PLUGGED_IN` |
| User restrictions | `addUserRestriction()` with `DISALLOW_SAFE_BOOT`, `DISALLOW_FACTORY_RESET`, `DISALLOW_ADD_USER`, and `DISALLOW_MOUNT_PHYSICAL_MEDIA` |

Clear waits for lock task mode to stop, and removes the lock task permission first.  
On Android 14 and later, setting the lock task features applies the permission again, and a locked task that loses the permission is closed.

| Source | Kind | Contents |
|---|---|---|
| `Components/DeviceOwner.android.cs` | 📝 App | Device policy to apply and clear (`PlatformApplyPolicy`, `PlatformClearPolicy`) |
| `Components/LockTask.cs` | 🧱 Base | Waiting for lock task mode to stop (`StopAsync`) |
| `Modules/Kiosk/KioskAdminViewModel.cs` | 📝 App | Apply and Clear |

```csharp
policyManager.SetLockTaskFeatures(admin, LockTaskFeatures.SystemInfo | LockTaskFeatures.GlobalActions);
policyManager.SetKeyguardDisabled(admin, true);
policyManager.AddUserRestriction(admin, UserManager.DisallowFactoryReset);
```

### 🔌 Reboot

Reboot on the admin screen restarts the device after a confirmation.

| Source | Kind | Contents |
|---|---|---|
| `Components/DeviceOwner.android.cs` | 📝 App | `DevicePolicyManager.reboot()` |
| `Modules/Kiosk/KioskAdminViewModel.cs` | 📝 App | Confirmation and reboot |

```csharp
if (await dialog.ConfirmAsync("Reboot the device?"))
{
    deviceOwner.Reboot();
}
```

### 🧪 Test configuration

The device owner sets the managed configurations of its own app, to try them without an MDM.

| Source | Kind | Contents |
|---|---|---|
| `Components/DeviceOwner.android.cs` | 📝 App | `DevicePolicyManager.setApplicationRestrictions()` (`PlatformSendConfiguration`) |
| `Modules/Kiosk/KioskAdminViewModel.cs` | 📝 App | Send and Clear |

```csharp
bundle.PutString(ManagedConfiguration.AdminPinKey, settings.AdminPin);
policyManager.SetApplicationRestrictions(admin, Application.Context.PackageName, bundle);
```

### 💾 Backup

The app data of a dedicated device is not backed up or transferred.

| Source | Kind | Contents |
|---|---|---|
| `Platforms/Android/AndroidManifest.xml` | 🧱 Base | `android:allowBackup="false"` and `android:dataExtractionRules` |
| `Platforms/Android/Resources/xml/data_extraction_rules.xml` | 🧱 Base | All domains excluded from the cloud backup and the device transfer |

```xml
<application android:allowBackup="false" android:dataExtractionRules="@xml/data_extraction_rules" ... />
```

## 🔄 How it works

### Used from an MDM

1. The MDM installs the app, allows it for lock task mode (kiosk mode), and sets the managed configurations
2. The app enters lock task mode at launch, and hides the system bars and keeps the screen on by the configurations
3. A change of the configurations is applied right away

### As a simple MDM

1. The app is set as the device owner
2. Apply on the admin screen applies the device policy, fixes the app as the home app, and starts lock task mode
3. After a reboot, the app starts in lock task mode without the lock screen

### Maintenance

1. Admin on the menu opens the admin screen after the PIN (`0000` by default)
2. Stop ends lock task mode (as the device owner, it also removes the permission), and Open settings opens the Android settings
3. Start returns to lock task mode, and Clear removes the device policy

## 🔧 Setup

### Device owner

Use a device without accounts, such as an emulator, because `dpm set-device-owner` fails when an account exists.  
Install the Debug build, and set the app as the device owner.

```
adb shell dpm set-device-owner template.kioskapp/template.mobileapp.KioskAdminReceiver
```

Open Admin on the menu with the PIN `0000`, and press Apply.

### Removing the device owner

Press Clear on the admin screen before removing the device owner.  
Removing it while the device policy is applied leaves the app as the home app.

```
adb shell dpm remove-active-admin template.kioskapp/template.mobileapp.KioskAdminReceiver
```

### MDM

Register the app in the MDM, allow it for lock task mode, and set the keys in the managed configurations table.

## 🔗 Links

- [template-maui](https://github.com/usausa/template-maui)
- [Lock task mode](https://developer.android.com/work/dpc/dedicated-devices/lock-task-mode)
- [Dedicated devices cookbook](https://developer.android.com/work/dpc/dedicated-devices/cookbook)
- [Managed configurations](https://developer.android.com/work/managed-configurations)
