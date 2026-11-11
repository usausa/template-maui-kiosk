# QR でのデバイスオーナーの設定

## 実現すること

- 新しい端末か初期化した端末で QR コードを読ませると、端末がこのアプリを落として入れ、デバイスオーナーにする
- アプリは完全に管理する端末を選び(`GET_PROVISIONING_MODE`)、設定の終わりに管理者の追加情報を当てる(`ADMIN_POLICY_COMPLIANCE`)
- 追加情報は構成のキーと `ApplyPolicy`(`true` なら端末の制限まで掛ける)
- QR コードの中身(JSON と PNG)は `tools/Template.Tools.ProvisioningQr` で作る(`appsettings.json` かコマンドライン)

## 確かめ方

前提: [エミュレーターの準備](emulator.md) と [試しのプログラム](emulator.md#6-試しのプログラム) を済ませ、QR 用の AVD(`KioskQr_API35`)を作っておく。
エミュレーターにはカメラで QR を読むセットアップの画面が無いので、QR と同じ中身を試しのプログラムで設定の仕組みに渡す。

### 準備

1. 署名した Release の版を作る → `Template.MobileApp\bin\Release\net10.0-android\android-x64\template.kioskapp-Signed.apk`

   ```powershell
   dotnet build Template.MobileApp/Template.MobileApp.csproj -c Release -f net10.0-android -p:RuntimeIdentifier=android-x64 --no-incremental
   ```

2. 別の PowerShell で、APK を手元の HTTP で配る(Ctrl+C で止める)

   ```powershell
   .\tools\emulator\serve.ps1 -Root Template.MobileApp\bin\Release\net10.0-android\android-x64
   ```

3. QR の中身を作る → `__Emulator\provisioning.json` と `provisioning.png`

   ```powershell
   dotnet run --project tools/Template.Tools.ProvisioningQr -- --Provisioning:DownloadLocation=http://127.0.0.1:18080/template.kioskapp-Signed.apk "--Provisioning:AdminExtras:Message=Provisioned by QR" --Output=__Emulator\provisioning
   ```

### 設定

1. QR 用の AVD を初期化して起動し、起動し終えたらすぐ root にして、エミュレーター用のセットアップを止める(遅れるとセットアップが済んだ扱いになるので、初期化からやり直す)

   ```powershell
   Start-Process $emulator -ArgumentList "-avd KioskQr_API35 -port 5560 -wipe-data"
   & $adb -s emulator-5560 wait-for-device
   do { Start-Sleep -Seconds 1 } until ((& $adb -s emulator-5560 shell getprop sys.boot_completed) -match "1")
   & $adb -s emulator-5560 root
   & $adb -s emulator-5560 wait-for-device
   & $adb -s emulator-5560 shell pm disable com.google.android.googlesdksetup
   ```

2. Wi-Fi を AndroidWifi につなぐ(設定は APK を落とす前にネットワークにつながっているかを見る。起動の直後は一度つながっても切れることがあるので、10 秒後もつながっているまで繰り返す)

   ```powershell
   & $adb -s emulator-5560 shell cmd wifi set-wifi-enabled enabled
   do {
       & $adb -s emulator-5560 shell cmd wifi connect-network AndroidWifi open
       Start-Sleep -Seconds 10
   } until ((& $adb -s emulator-5560 shell cmd wifi status) -match 'connected to "AndroidWifi"')
   ```

3. 端末からホストの HTTP に届くようにし、試しのプログラムと QR の中身を送る

   ```powershell
   & $adb -s emulator-5560 reverse tcp:18080 tcp:18080
   & $adb -s emulator-5560 push __Emulator\kiosk-tools.dex /data/local/tmp/kiosk-tools.dex
   & $adb -s emulator-5560 push __Emulator\provisioning.json /data/local/tmp/provisioning.json
   ```

4. QR を読んだ時と同じ設定を始める → エミュレーターに設定の画面が出る

   ```powershell
   & $adb -s emulator-5560 shell app_process -cp /data/local/tmp/kiosk-tools.dex /data/local/tmp Provision /data/local/tmp/provisioning.json
   ```

5. 画面に従って進める →「This device belongs to your organization」で Next →「Let's set up your work device」で Accept & continue → APK を落として入れ、デバイスオーナーにするまで待つ →「This device isn't private」で Next(Wi-Fi を選ぶ画面が出たら `AndroidWifi` を選ぶ)
6. 設定の仕上げを始める(本当の端末ではセットアップの画面が送る)→ ログに `Provisioning completed. applyPolicy=[True]`

   ```powershell
   & $adb -s emulator-5560 shell am start -a android.app.action.PROVISION_FINALIZATION
   & $adb -s emulator-5560 logcat -d | Select-String "Provisioning completed"
   ```

7. セットアップの完了の印を付ける

   ```powershell
   & $adb -s emulator-5560 shell settings put secure user_setup_complete 1
   & $adb -s emulator-5560 shell settings put global device_provisioned 1
   ```

### 結果

1. ツールバーの HOME(○)を押す → KioskApp がロックタスク(全画面)で立ち上がり、Menu に `Provisioned by QR`
2. Status を開く → Device owner が `Yes`、Admin PIN・Keep screen on・Hide system bars・Update window が `MDM`、System update が `02:00-04:00`
3. Device policy を PIN `0000` で開こうとする →「PIN is incorrect」。PIN `1234`(`appsettings.json` の値)で開ける → State が `Applied`

- 続けて [自分自身の更新](app-update.md) を行うなら、エミュレーターはそのままにする
- Release の版は `dpm remove-active-admin` で外せないので、やり直す時は初期化から行う

### 実機

1. `appsettings.json` に APK の URL(HTTPS)と署名の証明書の SHA-256(`keytool -list -v`)を入れ、ツールを実行する → `provisioning.png`
2. 新しい端末か初期化した端末で、ようこそ画面を 6 回押し、QR コードを読ませる
