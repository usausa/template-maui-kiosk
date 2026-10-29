# Template project for MAUI kiosk

English | [日本語](README.ja.md)

A MAUI template for Android kiosk apps on dedicated devices.  
It is used from an MDM, or works as a simple MDM with the app as the device owner.

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
| Menu | `Modules/Main/MenuView.xaml` | Message from the configurations, and the Status and Admin buttons |
| Status | `Modules/Kiosk/KioskStatusView.xaml` | Device owner, lock task, configuration values, and version |
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
| 📝 | `Components/DeviceOwner.android.cs` | Allowing itself (`setLockTaskPackages()`) |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | Start, Stop |

```csharp
[Activity(LaunchMode = LaunchMode.SingleTask, LockTaskMode = "if_whitelisted", ...)]
public sealed class MainActivity : MauiAppCompatActivity

policyManager.SetLockTaskPackages(admin, [Application.Context.PackageName!]);
ActivityResolver.CurrentActivity.StartLockTask();
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
| Lock task permission | `setLockTaskPackages()` | This app |
| System info | `setLockTaskFeatures()` | `LOCK_TASK_FEATURE_SYSTEM_INFO` |
| Power menu | `setLockTaskFeatures()` | `LOCK_TASK_FEATURE_GLOBAL_ACTIONS` |
| Home app | `addPersistentPreferredActivity()` | `HomeActivity` |
| No lock screen (without a PIN) | `setKeyguardDisabled()` | `true` |
| Screen on while charging | `setGlobalSetting()` | `STAY_ON_WHILE_PLUGGED_IN` |
| No safe boot | `addUserRestriction()` | `DISALLOW_SAFE_BOOT` |
| No factory reset | `addUserRestriction()` | `DISALLOW_FACTORY_RESET` |
| No new users | `addUserRestriction()` | `DISALLOW_ADD_USER` |
| No external media | `addUserRestriction()` | `DISALLOW_MOUNT_PHYSICAL_MEDIA` |

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
2. The app enters lock task mode at launch
3. Configuration changes are applied right away

### As a simple MDM

1. Set the app as the device owner
2. Apply on the admin screen sets the device policy, and enters lock task mode
3. After a reboot, the app starts in lock task mode

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

### Removing the device owner

Press Clear on the admin screen first (removing it with the policy applied leaves the app as the home app).

```
adb shell dpm remove-active-admin template.kioskapp/template.mobileapp.KioskAdminReceiver
```

### MDM

Register the app in the MDM, allow lock task mode, and set the configuration keys.

## 🔗 Links

- [template-maui](https://github.com/usausa/template-maui)
