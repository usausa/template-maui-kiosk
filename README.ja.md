# Template project for MAUI kiosk

[English](README.md) | 日本語

Android の専用端末向けのキオスクアプリの MAUI のテンプレート。  
MDM から使うほか、アプリ自身がデバイスオーナーになる簡易 MDM としても動く。

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
| Menu | `Modules/Main/MenuView.xaml` | 構成の知らせ、Status と Admin のボタン |
| Status | `Modules/Kiosk/KioskStatusView.xaml` | デバイスオーナー、ロックタスク、構成の値、版 |
| Admin | `Modules/Kiosk/KioskAdminView.xaml` | ロックタスク、端末の制限、再起動、構成の試し |

## 🧩 Android の技術

状態は `State/KioskState.cs` にまとめる。  
種別: 🧱 そのままコピーして使える / 📝 アプリに合わせて直す

### 📋 管理対象の構成

MDM が設定した値をアプリが読む。  
設定されていないキーは返らないので、既定値はアプリが持つ。  
参考: [Managed configurations](https://developer.android.com/work/managed-configurations)

| 種別 | ソース | 内容 |
|---|---|---|
| 📝 | `Platforms/Android/Resources/xml/app_restrictions.xml` | 項目の宣言 |
| 🧱 | `Platforms/Android/MainApplication.cs` | 宣言の登録(meta-data) |
| 📝 | `Components/ManagedConfiguration.cs` | キーと読み直し |
| 📝 | `Components/ManagedConfiguration.android.cs` | 読み取りと変更の受け取り |
| 📝 | `State/KioskState.cs` | 既定値 |
| 📝 | `MainPageViewModel.cs` | 前面に戻った時の読み直し(`OnResumed`) |

| キー | 型 | 既定値 | 用途 |
|---|---|---|---|
| `AdminPin` | string | `0000` | 保守の画面の PIN |
| `KeepScreenOn` | bool | `true` | ロックタスクの間、画面を点けたまま |
| `HideSystemBars` | bool | `true` | ロックタスクの間、システムバーを隠す |
| `Message` | string | | Menu に出す知らせ |

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

デバイス管理の受け手を持ち、`dpm set-device-owner` でデバイスオーナーになる。  
Debug のビルドは testOnly なので、`dpm remove-active-admin` で外せる。

| 種別 | ソース | 内容 |
|---|---|---|
| 🧱 | `Platforms/Android/KioskAdminReceiver.cs` | デバイス管理の受け手 |
| 🧱 | `Platforms/Android/Resources/xml/device_admin.xml` | 使う方針(無し) |
| 🧱 | `Platforms/Android/AndroidManifest.xml` | `android:testOnly` |
| 🧱 | `Template.MobileApp.csproj` | Debug だけ testOnly |
| 📝 | `Components/DeviceOwner.cs` | 公開する操作 |
| 📝 | `Components/DeviceOwner.android.cs` | デバイスオーナーかの判定 |

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

許可されたアプリは起動でロックタスクに入り、HOME や最近のアプリで出られない。  
許可が無い時は画面の固定になる(利用者が確認して始め、外せる)。  
参考: [Lock task mode](https://developer.android.com/work/dpc/dedicated-devices/lock-task-mode)

| 種別 | ソース | 内容 |
|---|---|---|
| 🧱 | `Platforms/Android/MainActivity.cs` | `LockTaskMode = "if_whitelisted"`・`SingleTask` |
| 🧱 | `Platforms/Android/KioskAdminReceiver.cs` | 出入りの知らせ |
| 🧱 | `Components/LockTask.cs` | 状態の見張り、止まるのを待つ(`StopAsync`) |
| 🧱 | `Components/LockTask.android.cs` | `startLockTask()`・`stopLockTask()`・状態・許可 |
| 📝 | `Components/DeviceOwner.android.cs` | 自分を許可(`setLockTaskPackages()`) |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | Start・Stop |

```csharp
[Activity(LaunchMode = LaunchMode.SingleTask, LockTaskMode = "if_whitelisted", ...)]
public sealed class MainActivity : MauiAppCompatActivity

policyManager.SetLockTaskPackages(admin, [Application.Context.PackageName!]);
ActivityResolver.CurrentActivity.StartLockTask();
```

### 📺 全画面と点灯

ロックタスクの間、構成に従ってシステムバーを隠し、画面を点けたままにする。  
システムバーはスワイプで一時的に出る。

| 種別 | ソース | 内容 |
|---|---|---|
| 📝 | `Platforms/Android/MainActivity.cs` | 窓の切り替え(`UpdateKioskWindow`) |

```csharp
window.AddFlags(WindowManagerFlags.KeepScreenOn);
controller.SystemBarsBehavior = WindowInsetsControllerCompat.BehaviorShowTransientBarsBySwipe;
controller.Hide(WindowInsetsCompat.Type.SystemBars());
```

### 🔑 保守の画面

Menu の Admin は、PIN を入れてから保守の画面を開く。  
保守の画面でロックタスクを止め、Android の設定を開ける。

| 種別 | ソース | 内容 |
|---|---|---|
| 📝 | `Modules/Main/MenuViewModel.cs` | PIN の照合 |
| 🧱 | `Modules/PopupNavigatorExtensions.cs` | PIN の入力(`InputPinAsync`) |
| 🧱 | `Modules/Navigation/Modal/InputNumberView.xaml` | 数字の入力のポップアップ |
| 🧱 | `Modules/Navigation/Modal/InputNumberViewModel.cs` | 伏せ字、先頭の 0 を残す |
| 🧱 | `Models/Input/` | 入力の値 |
| 📝 | `Modules/Kiosk/KioskAdminView.xaml` | 保守の画面 |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | 保守の画面の操作 |
| 🧱 | `Platforms/Android/AndroidHelper.cs` | 設定を開く(`OpenSettings`) |

```csharp
var pin = await popupNavigator.InputPinAsync("Admin PIN", Length.AdminPin);
if (pin == Kiosk.AdminPin)
{
    await Navigator.ForwardAsync(ViewId.KioskAdmin);
}
```

### 🏠 ホームアプリ

`HomeActivity` は HOME を受けて `MainActivity` を開くだけの中継で、既定では無効。  
Apply で有効にしてホームに固定するので、再起動の後もアプリが立ち上がる。  
`MainActivity` を直接ホームにしないのは、HOME で別のタスクにもう 1 つでき、常にホームの候補に出てしまうため。  
既定で無効のホーム用の Activity を有効にして固定する形は、[Test DPC](https://github.com/googlesamples/android-testdpc) の `KioskModeActivity` と同じ。

| 種別 | ソース | 内容 |
|---|---|---|
| 🧱 | `Platforms/Android/HomeActivity.cs` | HOME の中継(既定で無効) |
| 📝 | `Components/DeviceOwner.android.cs` | 有効にしてホームに固定 |

```csharp
[Activity(Theme = "@android:style/Theme.NoDisplay", TaskAffinity = "", Enabled = false, Exported = true)]
[IntentFilter([Intent.ActionMain], Categories = [Intent.CategoryHome, Intent.CategoryDefault])]
public sealed class HomeActivity : Activity

packageManager.SetComponentEnabledSetting(home, ComponentEnabledState.Enabled, ComponentEnableOption.DontKillApp);
policyManager.AddPersistentPreferredActivity(admin, filter, home);
```

### 📜 端末の制限

Apply はデバイスオーナーとして次を掛け、Clear は外す。  
参考: [Dedicated devices cookbook](https://developer.android.com/work/dpc/dedicated-devices/cookbook)

| 項目 | API | 値 |
|---|---|---|
| ロックタスクの許可 | `setLockTaskPackages()` | このアプリ |
| 時刻などの表示 | `setLockTaskFeatures()` | `LOCK_TASK_FEATURE_SYSTEM_INFO` |
| 電源メニュー | `setLockTaskFeatures()` | `LOCK_TASK_FEATURE_GLOBAL_ACTIONS` |
| ホームアプリ | `addPersistentPreferredActivity()` | `HomeActivity` |
| ロック画面の無効(PIN 無し) | `setKeyguardDisabled()` | `true` |
| 充電中は消灯しない | `setGlobalSetting()` | `STAY_ON_WHILE_PLUGGED_IN` |
| セーフモードの禁止 | `addUserRestriction()` | `DISALLOW_SAFE_BOOT` |
| 初期化の禁止 | `addUserRestriction()` | `DISALLOW_FACTORY_RESET` |
| ユーザーの追加の禁止 | `addUserRestriction()` | `DISALLOW_ADD_USER` |
| 外部メディアの禁止 | `addUserRestriction()` | `DISALLOW_MOUNT_PHYSICAL_MEDIA` |

Clear はロックタスクが止まるのを待ち、許可を最初に外す(順を変えるとアプリが閉じられる)。  
充電中の点灯は、Apply の前の値に戻す。

| 種別 | ソース | 内容 |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | `PlatformApplyPolicy`・`PlatformClearPolicy` |
| 🧱 | `Components/LockTask.cs` | 止まるのを待つ(`StopAsync`) |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | Apply・Clear |

```csharp
policyManager.SetLockTaskFeatures(admin, LockTaskFeatures.SystemInfo | LockTaskFeatures.GlobalActions);
policyManager.SetKeyguardDisabled(admin, true);
policyManager.AddUserRestriction(admin, UserManager.DisallowFactoryReset);
```

### 🔌 再起動

Reboot は確認してから端末を再起動する。

| 種別 | ソース | 内容 |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | 再起動(`reboot()`) |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | 確認 |

```csharp
if (await dialog.ConfirmAsync("Reboot the device?"))
{
    deviceOwner.Reboot();
}
```

### 🧪 構成の試し

デバイスオーナーが自分に構成を設定し、MDM 無しで試す。

| 種別 | ソース | 内容 |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | 設定(`setApplicationRestrictions()`) |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | Send・Clear |

```csharp
bundle.PutString(ManagedConfiguration.AdminPinKey, settings.AdminPin);
policyManager.SetApplicationRestrictions(admin, Application.Context.PackageName, bundle);
```

### 💾 バックアップ

アプリのデータはバックアップも移行もしない。

| 種別 | ソース | 内容 |
|---|---|---|
| 🧱 | `Platforms/Android/AndroidManifest.xml` | `allowBackup="false"`・`dataExtractionRules` |
| 🧱 | `Platforms/Android/Resources/xml/data_extraction_rules.xml` | すべての領域を外す |

```xml
<application android:allowBackup="false" android:dataExtractionRules="@xml/data_extraction_rules" ... />
```

## 🔄 動き

### MDM から使う

1. MDM がアプリを入れ、ロックタスクを許可し、構成を設定する
2. アプリは起動でロックタスクに入る
3. 構成の変更はその場で反映する

### 簡易 MDM として使う

1. アプリをデバイスオーナーにする
2. Admin の Apply で端末を制限し、ロックタスクに入る
3. 再起動の後もロックタスクで立ち上がる

### 保守

1. Menu の Admin で PIN(既定は `0000`)を入れる
2. Stop でロックタスクを止め、Open settings で設定を開く
3. Start で戻り、Clear で制限を外す

## 🔧 セットアップ

### デバイスオーナー

アカウントの無い端末(エミュレーターなど)に Debug のビルドを入れ、次を実行する。

```
adb shell dpm set-device-owner template.kioskapp/template.mobileapp.KioskAdminReceiver
```

Menu の Admin を PIN `0000` で開き、Apply を押す。

### デバイスオーナーを外す

先に Admin で Clear を押す(掛けたまま外すと、ホームがアプリのまま残る)。

```
adb shell dpm remove-active-admin template.kioskapp/template.mobileapp.KioskAdminReceiver
```

### MDM

MDM にアプリを登録し、ロックタスクを許可して、構成のキーを設定する。

## 🔗 リンク

- [template-maui](https://github.com/usausa/template-maui)
