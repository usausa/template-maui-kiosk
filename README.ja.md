# Template project for MAUI kiosk

[English](README.md) | 日本語

Android の専用端末向けのキオスクアプリの MAUI のテンプレート。  
MDM から使うほか、アプリ自身がデバイスオーナーになる簡易 MDM としても動く。

## ✨ 主な機能

- 管理対象の構成
- ロックタスク
- ロックタスクの間のほかのアプリ
- PIN で入る保守の画面
- ロックタスクの間の全画面
- デバイスオーナーとしての端末の制限
- システムの更新の時間帯
- ホームアプリ
- 再起動
- MDM への状態の報告
- QR コードでのデバイスオーナーの設定

## 📱 画面

| 画面 | ソース | 内容 |
|---|---|---|
| Menu | `Modules/Main/MenuView.xaml` | 構成の知らせ、Status・Apps・Admin のボタン |
| Status | `Modules/Kiosk/KioskStatusView.xaml` | 端末の状態、構成の値、状態の報告、版 |
| Apps | `Modules/Kiosk/KioskAppsView.xaml` | 許可したアプリを開くボタン |
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
| `SystemUpdateWindow` | string | `02:00-04:00` | 自動のシステムの更新の時間帯 |
| `LockTaskPackages` | string | | ロックタスクの間のほかのアプリ(カンマ区切り) |

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

### 📷 QR コードでの設定

新しい端末か初期化した端末で、ようこそ画面を 6 回押すと QR コードの読み取りが開き、端末はアプリを入れてデバイスオーナーにする。  
アプリは完全に管理する端末のモードを返し(`GET_PROVISIONING_MODE`)、設定の終わりに管理者の追加情報を当てる(`ADMIN_POLICY_COMPLIANCE`)。  
追加情報には構成のキーと `ApplyPolicy`(`true` なら端末の制限も掛ける)を入れる。  
`PROVISIONING_ALLOW_OFFLINE` は、端末管理のアプリの更新に失敗しても設定を続ける。  
参考: [Provision devices](https://developers.google.com/android/work/play/emm-api/prov-devices#qr_code_method)

| 種別 | ソース | 内容 |
|---|---|---|
| 🧱 | `Platforms/Android/ProvisioningModeActivity.cs` | 完全に管理する端末のモード(`GET_PROVISIONING_MODE`) |
| 🧱 | `Platforms/Android/PolicyComplianceActivity.cs` | 管理者の追加情報(`ADMIN_POLICY_COMPLIANCE`) |
| 📝 | `Components/DeviceOwner.cs` | 構成と端末の制限(`CompleteProvisioning`) |
| 📝 | `tools/Template.Tools.ProvisioningQr/appsettings.json` | QR コードの中身 |
| 🧱 | `tools/Template.Tools.ProvisioningQr/Program.cs` | QR コードの JSON と PNG |

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
| 📝 | `Components/DeviceOwner.android.cs` | 自分とほかのアプリを許可(`setLockTaskPackages()`) |
| 📝 | `Modules/Kiosk/KioskAdminViewModel.cs` | Start・Stop |

```csharp
[Activity(LaunchMode = LaunchMode.SingleTask, LockTaskMode = "if_whitelisted", ...)]
public sealed class MainActivity : MauiAppCompatActivity

policyManager.SetLockTaskPackages(admin, [Application.Context.PackageName!]);
ActivityResolver.CurrentActivity.StartLockTask();
```

### 📦 ほかのアプリ

構成の `LockTaskPackages` のアプリも、このアプリと一緒にロックタスクの間に許可する。  
Menu の Apps から開き、Back でアプリに戻る。  
ほかのアプリの名前を読むには、ランチャーのアプリの `<queries>` が要る。  
参考: [Package visibility filtering on Android](https://developer.android.com/training/package-visibility)

| 種別 | ソース | 内容 |
|---|---|---|
| 📝 | `Components/DeviceOwner.android.cs` | アプリの許可(`setLockTaskPackages()`) |
| 📝 | `Modules/Kiosk/KioskAppsView.xaml` | Apps の画面 |
| 📝 | `Modules/Kiosk/KioskAppsViewModel.cs` | 許可したアプリと起動 |
| 🧱 | `Platforms/Android/AndroidHelper.cs` | 名前と起動(`FindAppLabel`・`LaunchApp`) |
| 🧱 | `Platforms/Android/AndroidManifest.xml` | ランチャーのアプリの `<queries>` |

```csharp
policyManager.SetLockTaskPackages(admin, [Application.Context.PackageName!, .. lockTaskPackages]);

using var intent = packageManager.GetLaunchIntentForPackage(packageName);
intent.AddFlags(ActivityFlags.NewTask);
Application.Context.StartActivity(intent);
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
| ロックタスクの許可 | `setLockTaskPackages()` | このアプリとほかのアプリ |
| 時刻などの表示 | `setLockTaskFeatures()` | `LOCK_TASK_FEATURE_SYSTEM_INFO` |
| 電源メニュー | `setLockTaskFeatures()` | `LOCK_TASK_FEATURE_GLOBAL_ACTIONS` |
| ホームアプリ | `addPersistentPreferredActivity()` | `HomeActivity` |
| ロック画面の無効(PIN 無し) | `setKeyguardDisabled()` | `true` |
| 充電中は消灯しない | `setGlobalSetting()` | `STAY_ON_WHILE_PLUGGED_IN` |
| セーフモードの禁止 | `addUserRestriction()` | `DISALLOW_SAFE_BOOT` |
| 初期化の禁止 | `addUserRestriction()` | `DISALLOW_FACTORY_RESET` |
| ユーザーの追加の禁止 | `addUserRestriction()` | `DISALLOW_ADD_USER` |
| 外部メディアの禁止 | `addUserRestriction()` | `DISALLOW_MOUNT_PHYSICAL_MEDIA` |
| システムの更新 | `setSystemUpdatePolicy()` | 構成の時間帯 |

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

### 🕑 システムの更新の時間帯

Apply は、自動のシステムの更新を、構成の `SystemUpdateWindow`(`HH:mm-HH:mm`、既定は `02:00-04:00`)の毎日の時間帯に限る。  
方針を掛けている間に構成が変わると新しい時間帯で掛け直し、Clear で外す。  
時間帯は 0 時からの分(`120`〜`240`)で、開始が終わりより後なら日付をまたぐ。  
参考: [Manage system updates](https://developer.android.com/work/dpc/system-updates)

| 種別 | ソース | 内容 |
|---|---|---|
| 🧱 | `Components/SystemUpdateWindow.cs` | 時間帯(`HH:mm-HH:mm`) |
| 📝 | `Components/DeviceOwner.cs` | 構成が変わった時の掛け直し |
| 📝 | `Components/DeviceOwner.android.cs` | 方針の設定と読み取り |
| 📝 | `Modules/Kiosk/KioskStatusView.xaml` | 今の方針(System update) |

```csharp
using var policy = SystemUpdatePolicy.CreateWindowedInstallPolicy(120, 240);
policyManager.SetSystemUpdatePolicy(admin, policy);
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

### 📨 状態の報告

アプリは、起動の時と変わった時に、状態を Keyed App States で MDM に報告する。  
簡易 MDM としては、アプリ自身がデバイスオーナーとして受け取り、最後に受けた状態を Status に出す。  
参考: [Send app feedback to EMMs](https://developer.android.com/work/app-feedback/overview)

| キー | 重要度 | 内容 |
|---|---|---|
| `LockTask` | Info | `None`・`Locked`・`Pinned` |
| `DevicePolicy` | Info | `Applied`・`NotApplied` |
| `AdminPin` | Info / Error | PIN が数字でない時は Error |
| `SystemUpdateWindow` | Info / Error | `HH:mm-HH:mm` でない時は Error |

| 種別 | ソース | 内容 |
|---|---|---|
| 🧱 | `Template.MobileApp.csproj` | `Xamarin.AndroidX.Enterprise.Feedback` |
| 🧱 | `Components/AppFeedback.cs` | 状態・報告・受け取り |
| 🧱 | `Components/AppFeedback.android.cs` | 報告(`KeyedAppStatesReporter`) |
| 🧱 | `Platforms/Android/KioskAppStatesService.cs` | 受け取り(`KeyedAppStatesService`) |
| 📝 | `State/KioskState.cs` | 報告する状態 |

```csharp
reporter.SetStates(keyedStates, null);

[Service(Name = "template.mobileapp.KioskAppStatesService", Exported = true)]
[IntentFilter(["androidx.enterprise.feedback.action.APP_STATES"])]
public sealed class KioskAppStatesService : KeyedAppStatesService
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
2. アプリは起動で全画面のロックタスクに入る
3. 構成の変更はその場で反映する
4. アプリは状態を MDM に報告する

### 簡易 MDM として使う

1. QR コード(試しの端末では `dpm`)でアプリをデバイスオーナーにする
2. Admin の Apply で端末を制限し、全画面のロックタスクに入る
3. 再起動の後もロックタスクで立ち上がる
4. Menu の Apps から、許可したアプリを開く

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

### QR コード

`tools/Template.Tools.ProvisioningQr/appsettings.json` に APK の場所・署名の証明書の SHA-256(`keytool -list -v`)・管理者の追加情報を入れ、ツールを実行する。  
ツールは `provisioning.json` と `provisioning.png` を書き、値はコマンドラインでも渡せる。  
署名した Release のビルドを使う。

```
dotnet run --project tools/Template.Tools.ProvisioningQr -- --Provisioning:DownloadLocation=https://example.com/app.apk
```

新しい端末か初期化した端末で、ようこそ画面を 6 回押し、QR コードを読ませる。  
Release のビルドは `dpm remove-active-admin` で外せないので、設定し直す時は端末を初期化する。

### デバイスオーナーを外す

先に Admin で Clear を押す(掛けたまま外すと、ホームがアプリのまま残る)。

```
adb shell dpm remove-active-admin template.kioskapp/template.mobileapp.KioskAdminReceiver
```

### MDM

MDM にアプリを登録し、ロックタスクを許可して、構成のキーを設定する。

## 🔗 リンク

- [template-maui](https://github.com/usausa/template-maui)
