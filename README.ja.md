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

### 管理対象の構成

MDM がアプリの宣言した項目に値を設定し、アプリが読む。  
MDM が設定したキーだけが返るので、既定値はアプリが持つ。  
値は起動時と前面に戻った時に読み、変更は実行時に登録した受け手で受け取る。

- Android: `app_restrictions.xml` と `android.content.APP_RESTRICTIONS` の meta-data、`RestrictionsManager.getApplicationRestrictions()`、`ACTION_APPLICATION_RESTRICTIONS_CHANGED`
- ソース: `Platforms/Android/Resources/xml/app_restrictions.xml`、`Platforms/Android/MainApplication.cs`、`Components/ManagedConfiguration.cs` + `.android.cs`、`State/KioskState.cs`(既定値)、`MainPageViewModel.cs`(`OnResumed`)

| キー | 型 | 既定値 | 用途 |
|---|---|---|---|
| `AdminPin` | string | `0000` | 保守の画面の PIN |
| `KeepScreenOn` | bool | `true` | ロックタスクの間、画面を点けたままにする |
| `HideSystemBars` | bool | `true` | ロックタスクの間、システムバーを隠す |
| `Message` | string | | メニューに出す知らせ |

### デバイスオーナー

アプリはデバイス管理の受け手を持ち、`dpm set-device-owner` でデバイスオーナーになる。  
Debug のビルドはテスト用なので、`dpm remove-active-admin` でデバイスオーナーを外せる。

- Android: `DeviceAdminReceiver`、`device_admin.xml`、`DevicePolicyManager.isDeviceOwnerApp()`、`android:testOnly`
- ソース: `Platforms/Android/KioskAdminReceiver.cs`、`Platforms/Android/Resources/xml/device_admin.xml`、`Components/DeviceOwner.cs` + `.android.cs`、`Platforms/Android/AndroidManifest.xml` と `Template.MobileApp.csproj` の `AndroidManifestPlaceholders`(テスト用)

### ロックタスク

デバイスオーナーか MDM が許可したアプリは起動でロックタスクに入り、HOME や最近のアプリでアプリから出られない。  
許可が無い時の `startLockTask()` は画面の固定になり、利用者の確認で始まり、利用者が外せる。  
状態は `ActivityManager` から読み、変わったことはデバイス管理の受け手で受け取る(画面の固定は状態を見に行く)。

- Android: `DevicePolicyManager.setLockTaskPackages()`、`android:lockTaskMode="if_whitelisted"`、`Activity.startLockTask()` / `stopLockTask()`、`ActivityManager.getLockTaskModeState()`、`DeviceAdminReceiver.onLockTaskModeEntering()` / `onLockTaskModeExiting()`
- ソース: `Platforms/Android/MainActivity.cs`(`LockTaskMode`)、`Components/LockTask.cs` + `.android.cs`、`Platforms/Android/KioskAdminReceiver.cs`、`Modules/Kiosk/KioskAdminViewModel.cs`(Start・Stop)

### 全画面と点灯

ロックタスクの間は、システムバーを隠し、画面を点けたままにする。  
スワイプでシステムバーが一時的に出る。  
どちらも管理対象の構成に従う。

- Android: `WindowInsetsControllerCompat`(`BEHAVIOR_SHOW_TRANSIENT_BARS_BY_SWIPE`)、`FLAG_KEEP_SCREEN_ON`
- ソース: `Platforms/Android/MainActivity.cs`(`UpdateKioskWindow`)

### 保守の画面

メニューの Admin は、管理対象の構成の PIN を入れた後に保守の画面を開く。  
保守の画面では、保守のためにロックタスクを止め、Open settings で Android の設定を開く。

- Android: `Settings.ACTION_SETTINGS`
- ソース: `Modules/Main/MenuViewModel.cs`、`Modules/PopupNavigatorExtensions.cs`(`InputPinAsync`)、`Modules/Navigation/Modal/InputNumberView.xaml`(伏せ字の数字)、`Modules/Kiosk/KioskAdminView.xaml` + `KioskAdminViewModel.cs`、`Platforms/Android/AndroidHelper.cs`(`OpenSettings`)

### ホームアプリ

`HomeActivity` は HOME の intent-filter を持ち、既定で無効なので、普通の端末ではホームアプリの候補に出ない。  
デバイスオーナーが有効にしてホームアプリに固定するので、再起動の後にアプリが立ち上がる。  
`HomeActivity` は `MainActivity` を開いて閉じるだけで、`MainActivity` は標準のタスクに 1 つのままになる。

- Android: `ACTION_MAIN` + `CATEGORY_HOME` + `CATEGORY_DEFAULT`、`PackageManager.setComponentEnabledSetting()`、`DevicePolicyManager.addPersistentPreferredActivity()` / `clearPackagePersistentPreferredActivities()`
- ソース: `Platforms/Android/HomeActivity.cs`、`Components/DeviceOwner.android.cs`

### 端末の制限

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

- ソース: `Components/DeviceOwner.android.cs`(`PlatformApplyPolicy`・`PlatformClearPolicy`)、`Components/LockTask.cs`(`StopAsync`)、`Modules/Kiosk/KioskAdminViewModel.cs`(Apply・Clear)

### 再起動

保守の画面の Reboot は、確認の後に端末を再起動する。

- Android: `DevicePolicyManager.reboot()`
- ソース: `Components/DeviceOwner.android.cs`、`Modules/Kiosk/KioskAdminViewModel.cs`

### 構成の試し

デバイスオーナーが自分のアプリに管理対象の構成を設定し、MDM 無しで試す。

- Android: `DevicePolicyManager.setApplicationRestrictions()`
- ソース: `Components/DeviceOwner.android.cs`(`PlatformSendConfiguration`)、`Modules/Kiosk/KioskAdminViewModel.cs`(Send・Clear)

### バックアップ

専用端末のアプリのデータは、バックアップも移行もしない。

- Android: `android:allowBackup="false"`、`android:dataExtractionRules`
- ソース: `Platforms/Android/AndroidManifest.xml`、`Platforms/Android/Resources/xml/data_extraction_rules.xml`

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
