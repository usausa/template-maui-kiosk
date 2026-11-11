# エミュレーターの準備

## 1. 道具

### PowerShell

コマンドは Windows の PowerShell で、リポジトリの直下で実行する。

```powershell
Set-Location <リポジトリ>
$sdk = "C:\Program Files (x86)\Android\android-sdk"
$adb = "$sdk\platform-tools\adb.exe"
$emulator = "$sdk\emulator\emulator.exe"
$env:JAVA_HOME = "C:\Program Files\Android\Android Studio\jbr"
```

- `$sdk` は Android SDK の場所(Android Studio で入れた時は `$env:LOCALAPPDATA\Android\Sdk`)
- `JAVA_HOME` は JDK 17 以降(`sdkmanager`・`avdmanager` が使う。Microsoft OpenJDK でもよい)
- `tools\emulator\*.ps1` が止められる時は `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned`

## 2. AVD

### イメージ

```powershell
& "$sdk\cmdline-tools\latest\bin\sdkmanager.bat" "system-images;android-35;google_apis;x86_64"
```

- Play ストアの無い `google_apis` のイメージにする(アカウントが無く、`adb root` が使える)

### 作成

```powershell
"no" | & "$sdk\cmdline-tools\latest\bin\avdmanager.bat" create avd -n Kiosk_API35 -k "system-images;android-35;google_apis;x86_64" -d pixel_9
```

- 「custom hardware profile」を作るかを聞かれるので `no` を渡す
- [QR でのデバイスオーナーの設定](qr-provisioning.md) と [自分自身の更新](app-update.md) には、同じ形でもう 1 台(`KioskQr_API35`)を作る

### 起動

```powershell
Start-Process $emulator -ArgumentList "-avd Kiosk_API35 -port 5558"
& $adb -s emulator-5558 wait-for-device
```

- ほかの端末とぶつからないように、ポートを決めて `-s emulator-5558` で指す

## 3. アプリ

### 入れる

```powershell
dotnet build Template.MobileApp/Template.MobileApp.csproj -c Debug -f net10.0-android -p:RuntimeIdentifier=android-x64 -t:Install -p:AdbTarget="-s emulator-5558"
```

### デバイスオーナーにする

```powershell
& $adb -s emulator-5558 shell dpm set-device-owner template.kioskapp/template.mobileapp.KioskAdminReceiver
```

- 結果: `Success: Device owner set to package ...`
- Debug の版は testOnly なので、`dpm remove-active-admin` で外せる(先に Device policy の Clear を押す)

### 開く

```powershell
& $adb -s emulator-5558 shell am start -n template.kioskapp/template.mobileapp.MainActivity
```

- エミュレーターのアプリの一覧の KioskApp でもよい

### 止める

```powershell
$p = & $adb -s emulator-5558 shell pidof template.kioskapp
if ($p) { & $adb -s emulator-5558 shell run-as template.kioskapp kill $p }
```

- デバイスオーナーのアプリは `am force-stop` が効かない
- Debug の版を入れ直した後は、止めてから開く(動いているプロセスは前のコードのまま)

### 始めの状態

1. Menu の Status を開く → Device owner が `Yes`、Lock task が `None`、System update が `None`、Lock task apps が `0`(`Default`)

## 4. 操作

- 前の画面に戻る: 画面の下の赤い Back
- 保守の画面(Lock task・Device policy・App update・Test configuration)と Reboot: Menu のボタン → PIN `0000` → ✓
- 構成を試す: Test configuration に値を入れて Send(Clear ですべて外す)
- ほかのアプリから戻る: エミュレーターの横のツールバーの戻る(◁)
- HOME と履歴: ツールバーの ○ と □
- 全画面の案内: 新しい AVD で初めて全画面になると「Viewing full screen」が出るので、Got it で閉じる
- 標準のランチャーの停止: 「Pixel Launcher keeps stopping」が出たら Close app で閉じる。方針を掛けたまま再起動した後に Clear すると落ち続けるので、Menu の Reboot で再起動する

## 5. 後片付け

各手順書の確かめの後に、方針と構成を外した状態に戻す。

1. Device policy の Clear を押す(`Applied` の時。`Not applied` になるまで数秒から十数秒かかる)
2. Test configuration の Clear を押す

## 6. 試しのプログラム

QR を読む所の代わり・誤った構成の配り・権限の固定の印を外す所は、端末で root で動かす小さなプログラムで行う(`tools/emulator/*.java`)。

```powershell
.\tools\emulator\build-dex.ps1
```

- 結果: `__Emulator\kiosk-tools.dex`
- JDK 11 以降(`JAVA_HOME`・Android Studio の `jbr`・Microsoft OpenJDK の順に探す)と、Android SDK の build-tools の d8 を使う
- 端末へは `adb push` で送り、`adb root` の後に `app_process` で動かす(使い方は各手順書)
- 手元の HTTP の `serve.ps1` はホストの 18080 番で待つ。ほかのものが 18080 番を使っている時は `-Port 18081` を付け、端末への転送を `adb reverse tcp:18080 tcp:18081` にする(端末から見る URL は 18080 のまま)
