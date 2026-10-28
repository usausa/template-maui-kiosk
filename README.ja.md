# Template project for MAUI kiosk

[English](README.md) | 日本語

専用端末向けに、MDM から使われる、またはアプリ自身がデバイスオーナーになる簡易 MDM として動く、Android のキオスクアプリの MAUI のテンプレートプロジェクト。

## ✨ 主な機能

- 管理対象の構成
- ロックタスク
- PIN で入る保守の画面
- ロックタスクの間の全画面
- デバイスオーナーとしての端末の制限
- ホームアプリ
- 再起動

## 📱 画面

| 画面 | ソース | 内容 |
|---|---|---|
| Menu | `Modules/Main/MenuView.xaml` | 管理対象の構成の知らせと、Status・Admin のボタン |
| Status | `Modules/Kiosk/KioskStatusView.xaml` | デバイスオーナーか、ロックタスクの状態、ロックタスクの許可、管理対象の構成の値(MDM か既定値か)、版 |
| Admin | `Modules/Kiosk/KioskAdminView.xaml` | ロックタスク(Start・Stop・Open settings)、端末の制限(Device policy の Apply・Clear・Reboot)、構成の試し(Test configuration の Send・Clear) |

## 🧩 Android の技術

画面と `MainActivity` が使う状態は `State/KioskState.cs` にまとめる。  
Android の部分は `Components`(`.android.cs`)と `Platforms/Android` にある。  
表の種別は、🧱 基盤 がほかのアプリへそのままコピーして使えるもの、📝 アプリ がアプリに合わせて直すもの。

### 📋 管理対象の構成

MDM がアプリの宣言した項目に値を設定し、アプリが読む。  
MDM が設定したキーだけが返るので、既定値はアプリが持つ。  
値は起動時と前面に戻った時に読み、変更は実行時に登録した受け手で受け取る。

| ソース | 種別 | 内容 |
|---|---|---|
| `Platforms/Android/Resources/xml/app_restrictions.xml` | 📝 アプリ | 項目の宣言(`<restrictions>`) |
| `Platforms/Android/MainApplication.cs` | 🧱 基盤 | `android.content.APP_RESTRICTIONS` の meta-data |
| `Components/ManagedConfiguration.cs` + `.android.cs` | 📝 アプリ | `RestrictionsManager.getApplicationRestrictions()` での読み取りと、`ACTION_APPLICATION_RESTRICTIONS_CHANGED` の受け取り(キーはアプリのもの) |
| `State/KioskState.cs` | 📝 アプリ | 既定値 |
| `MainPageViewModel.cs` | 📝 アプリ | `OnResumed` での読み直し |

| キー | 型 | 既定値 | 用途 |
|---|---|---|---|
| `AdminPin` | string | `0000` | 保守の画面の PIN |
| `KeepScreenOn` | bool | `true` | ロックタスクの間、画面を点けたままにする |
| `HideSystemBars` | bool | `true` | ロックタスクの間、システムバーを隠す |
| `Message` | string | | メニューに出す知らせ |

```xml
<restriction android:key="AdminPin" android:title="Admin PIN" android:restrictionType="string" android:defaultValue="0000" />
```

```csharp
[MetaData("android.content.APP_RESTRICTIONS", Resource = "@xml/app_restrictions")]
public sealed class MainApplication : MauiApplication

using var bundle = restrictionsManager.ApplicationRestrictions;
var adminPin = bundle.ContainsKey(AdminPinKey) ? bundle.GetString(AdminPinKey) : null;
```

### 👑 デバイスオーナー

アプリはデバイス管理の受け手を持ち、`dpm set-device-owner` でデバイスオーナーになる。  
Debug のビルドはテスト用なので、`dpm remove-active-admin` でデバイスオーナーを外せる。

| ソース | 種別 | 内容 |
|---|---|---|
| `Platforms/Android/KioskAdminReceiver.cs` | 🧱 基盤 | `DeviceAdminReceiver` |
| `Platforms/Android/Resources/xml/device_admin.xml` | 🧱 基盤 | デバイス管理の方針(無し) |
| `Components/DeviceOwner.cs` + `.android.cs` | 📝 アプリ | `DevicePolicyManager.isDeviceOwnerApp()` での判定と、アプリの端末の制限とキー |
| `Platforms/Android/AndroidManifest.xml`、`Template.MobileApp.csproj` | 🧱 基盤 | Debug のビルドだけの `android:testOnly`(`AndroidManifestPlaceholders`) |

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

### 🔒 ロックタスク

デバイスオーナーか MDM が許可したアプリは起動でロックタスクに入り、HOME や最近のアプリでアプリから出られない。  
許可が無い時の `startLockTask()` は画面の固定になり、利用者の確認で始まり、利用者が外せる。  
状態は `ActivityManager` から読み、変わったことはデバイス管理の受け手で受け取る(画面の固定は状態を見に行く)。

| ソース | 種別 | 内容 |
|---|---|---|
| `Platforms/Android/MainActivity.cs` | 🧱 基盤 | `[Activity]` の `LockTaskMode = "if_whitelisted"` と `SingleTask` |
| `Components/LockTask.cs` + `.android.cs` | 🧱 基盤 | `Activity.startLockTask()` / `stopLockTask()`、`ActivityManager.getLockTaskModeState()`、`DevicePolicyManager.isLockTaskPermitted()`、止まるのを待つ(`StopAsync`) |
| `Platforms/Android/KioskAdminReceiver.cs` | 🧱 基盤 | `onLockTaskModeEntering()` / `onLockTaskModeExiting()` |
| `Components/DeviceOwner.android.cs` | 📝 アプリ | `DevicePolicyManager.setLockTaskPackages()` での許可 |
| `Modules/Kiosk/KioskAdminViewModel.cs` | 📝 アプリ | Start と Stop |

```csharp
[Activity(LaunchMode = LaunchMode.SingleTask, LockTaskMode = "if_whitelisted", ...)]
public sealed class MainActivity : MauiAppCompatActivity

policyManager.SetLockTaskPackages(admin, [Application.Context.PackageName!]);
ActivityResolver.CurrentActivity.StartLockTask();
```

### 📺 全画面と点灯

ロックタスクの間は、システムバーを隠し、画面を点けたままにする。  
スワイプでシステムバーが一時的に出る。  
どちらも管理対象の構成に従う。

| ソース | 種別 | 内容 |
|---|---|---|
| `Platforms/Android/MainActivity.cs`(`UpdateKioskWindow`) | 📝 アプリ | `KioskState` の状態に従った `WindowInsetsControllerCompat`(`BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE`)と `FLAG_KEEP_SCREEN_ON` |

```csharp
window.AddFlags(WindowManagerFlags.KeepScreenOn);
controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
controller.Hide(WindowInsetsCompat.Type.SystemBars());
```

### 🔑 保守の画面

メニューの Admin は、管理対象の構成の PIN を入れた後に保守の画面を開く。  
保守の画面では、保守のためにロックタスクを止め、Open settings で Android の設定を開く。

| ソース | 種別 | 内容 |
|---|---|---|
| `Modules/Main/MenuViewModel.cs` | 📝 アプリ | PIN の照合 |
| `Modules/PopupNavigatorExtensions.cs`、`Modules/Navigation/Modal/InputNumberView.xaml` + `InputNumberViewModel.cs`、`Models/Input/` | 🧱 基盤 | 伏せ字の数字での PIN の入力(`InputPinAsync`) |
| `Modules/Kiosk/KioskAdminView.xaml` + `KioskAdminViewModel.cs` | 📝 アプリ | 保守の画面 |
| `Platforms/Android/AndroidHelper.cs` | 🧱 基盤 | `Settings.ACTION_SETTINGS` での Android の設定(`OpenSettings`) |

```csharp
var pin = await popupNavigator.InputPinAsync("Admin PIN", Length.AdminPin);
if (pin == Kiosk.AdminPin)
{
    await Navigator.ForwardAsync(ViewId.KioskAdmin);
}
```

### 🏠 ホームアプリ

`HomeActivity` は HOME の intent-filter を持ち、既定で無効なので、普通の端末ではホームアプリの候補に出ない。  
デバイスオーナーが有効にしてホームアプリに固定するので、再起動の後にアプリが立ち上がる。  
`HomeActivity` は `MainActivity` を開いて閉じるだけで、`MainActivity` は標準のタスクに 1 つのままになる。

| ソース | 種別 | 内容 |
|---|---|---|
| `Platforms/Android/HomeActivity.cs` | 🧱 基盤 | `ACTION_MAIN` + `CATEGORY_HOME` + `CATEGORY_DEFAULT`、既定で無効、`MainActivity` を開く |
| `Components/DeviceOwner.android.cs` | 📝 アプリ | `PackageManager.setComponentEnabledSetting()`、`DevicePolicyManager.addPersistentPreferredActivity()` / `clearPackagePersistentPreferredActivities()` |

```csharp
[Activity(Theme = "@android:style/Theme.NoDisplay", TaskAffinity = "", Enabled = false, Exported = true)]
[IntentFilter([Intent.ActionMain], Categories = [Intent.CategoryHome, Intent.CategoryDefault])]
public sealed class HomeActivity : Activity

packageManager.SetComponentEnabledSetting(home, ComponentEnabledState.Enabled, ComponentEnableOption.DontKillApp);
policyManager.AddPersistentPreferredActivity(admin, filter, home);
```

### 📜 端末の制限

保守の画面の Apply は、デバイスオーナーとして次を掛け、Clear は外す。

| 制限 | Android |
|---|---|
| ロックタスクの許可 | `setLockTaskPackages()` |
| ロックタスクの機能(システムの情報と電源メニューだけ) | `setLockTaskFeatures()` の `LOCK_TASK_FEATURE_SYSTEM_INFO`・`LOCK_TASK_FEATURE_GLOBAL_ACTIONS` |
| ホームアプリ | `addPersistentPreferredActivity()` |
| ロック画面の無効(PIN やパスワードが無い時だけ) | `setKeyguardDisabled()` |
| 充電中の点灯 | `setGlobalSetting()` の `STAY_ON_WHILE_PLUGGED_IN` |
| ユーザーの制限 | `addUserRestriction()` の `DISALLOW_SAFE_BOOT`・`DISALLOW_FACTORY_RESET`・`DISALLOW_ADD_USER`・`DISALLOW_MOUNT_PHYSICAL_MEDIA` |

Clear はロックタスクが止まるのを待ち、ロックタスクの許可を先に外す。  
Android 14 以降はロックタスクの機能の設定で許可が当て直され、許可を失ったロック中のタスクは閉じられる。

| ソース | 種別 | 内容 |
|---|---|---|
| `Components/DeviceOwner.android.cs` | 📝 アプリ | 掛ける・外す端末の制限(`PlatformApplyPolicy`・`PlatformClearPolicy`) |
| `Components/LockTask.cs` | 🧱 基盤 | ロックタスクが止まるのを待つ(`StopAsync`) |
| `Modules/Kiosk/KioskAdminViewModel.cs` | 📝 アプリ | Apply と Clear |

```csharp
policyManager.SetLockTaskFeatures(admin, LockTaskFeatures.SystemInfo | LockTaskFeatures.GlobalActions);
policyManager.SetKeyguardDisabled(admin, true);
policyManager.AddUserRestriction(admin, UserManager.DisallowFactoryReset);
```

### 🔌 再起動

保守の画面の Reboot は、確認の後に端末を再起動する。

| ソース | 種別 | 内容 |
|---|---|---|
| `Components/DeviceOwner.android.cs` | 📝 アプリ | `DevicePolicyManager.reboot()` |
| `Modules/Kiosk/KioskAdminViewModel.cs` | 📝 アプリ | 確認と再起動 |

```csharp
if (await dialog.ConfirmAsync("Reboot the device?"))
{
    deviceOwner.Reboot();
}
```

### 🧪 構成の試し

デバイスオーナーが自分のアプリに管理対象の構成を設定し、MDM 無しで試す。

| ソース | 種別 | 内容 |
|---|---|---|
| `Components/DeviceOwner.android.cs` | 📝 アプリ | `DevicePolicyManager.setApplicationRestrictions()`(`PlatformSendConfiguration`) |
| `Modules/Kiosk/KioskAdminViewModel.cs` | 📝 アプリ | Send と Clear |

```csharp
bundle.PutString(ManagedConfiguration.AdminPinKey, settings.AdminPin);
policyManager.SetApplicationRestrictions(admin, Application.Context.PackageName, bundle);
```

### 💾 バックアップ

専用端末のアプリのデータは、バックアップも移行もしない。

| ソース | 種別 | 内容 |
|---|---|---|
| `Platforms/Android/AndroidManifest.xml` | 🧱 基盤 | `android:allowBackup="false"` と `android:dataExtractionRules` |
| `Platforms/Android/Resources/xml/data_extraction_rules.xml` | 🧱 基盤 | クラウドのバックアップと端末間の移行から、すべての領域を外す |

```xml
<application android:allowBackup="false" android:dataExtractionRules="@xml/data_extraction_rules" ... />
```

## 🔄 動き

### MDM から使う

1. MDM がアプリを入れ、ロックタスク(キオスクモード)を許可し、管理対象の構成を設定する
2. アプリは起動でロックタスクに入り、構成に従ってシステムバーを隠し、画面を点けたままにする
3. 構成の変更はその場で反映する

### 簡易 MDM として使う

1. アプリをデバイスオーナーにする
2. 保守の画面の Apply で端末の制限を掛け、アプリをホームアプリに固定し、ロックタスクを始める
3. 再起動の後、アプリはロック画面無しでロックタスクで立ち上がる

### 保守

1. メニューの Admin は PIN(既定は `0000`)の後に保守の画面を開く
2. Stop でロックタスクを終え(デバイスオーナーなら許可も外す)、Open settings で Android の設定を開く
3. Start でロックタスクに戻り、Clear で端末の制限を外す

## 🔧 セットアップ

### デバイスオーナー

アカウントがあると `dpm set-device-owner` は失敗するので、エミュレーターなどアカウントの無い端末を使う。  
Debug のビルドを入れ、アプリをデバイスオーナーにする。

```
adb shell dpm set-device-owner template.kioskapp/template.mobileapp.KioskAdminReceiver
```

メニューの Admin を PIN `0000` で開き、Apply を押す。

### デバイスオーナーを外す

デバイスオーナーを外す前に、保守の画面で Clear を押す。  
端末の制限を掛けたまま外すと、アプリがホームアプリのまま残る。

```
adb shell dpm remove-active-admin template.kioskapp/template.mobileapp.KioskAdminReceiver
```

### MDM

MDM にアプリを登録し、ロックタスクを許可し、管理対象の構成の表のキーを設定する。

## 🔗 リンク

- [template-maui](https://github.com/usausa/template-maui)
- [Lock task mode](https://developer.android.com/work/dpc/dedicated-devices/lock-task-mode)
- [Dedicated devices cookbook](https://developer.android.com/work/dpc/dedicated-devices/cookbook)
- [Managed configurations](https://developer.android.com/work/managed-configurations)
