# MDM への状態の報告

## 実現すること

- アプリの状態を Keyed App States で MDM に報告する(`Xamarin.AndroidX.Enterprise.Feedback` の `KeyedAppStatesReporter`)
- 報告するのは、ロックタスク(`LockTask`)・端末の制限(`DevicePolicy`)・構成の誤り(`AdminPin`・`SystemUpdateWindow`)。起動の時と変わった時に送る
- 誤った構成の時は `Error` と理由の文を送る
- 簡易 MDM として、アプリ自身がデバイスオーナーとして受け取り(`KeyedAppStatesService`)、Status の App feedback に最後に受けた状態を出す

## 確かめ方

前提: [エミュレーターの準備](emulator.md) と [試しのプログラム](emulator.md#6-試しのプログラム) を済ませる。

### 報告

1. Status を開き、下へスクロールする → App feedback に `AdminPin`・`DevicePolicy`・`LockTask`・`SystemUpdateWindow` がこの順に並び、どれも `Info`。DevicePolicy は `NotApplied`、LockTask は `None`
2. Device policy の Apply を押し、Status を開く → DevicePolicy が `Applied`、LockTask が `Locked`

### 誤った構成

Test configuration は誤った値を送れないので、試しのプログラムで MDM と同じように配る。

1. 誤った構成を配る

   ```powershell
   & $adb -s emulator-5558 root
   & $adb -s emulator-5558 wait-for-device
   & $adb -s emulator-5558 push __Emulator\kiosk-tools.dex /data/local/tmp/kiosk-tools.dex
   & $adb -s emulator-5558 shell app_process -cp /data/local/tmp/kiosk-tools.dex /data/local/tmp Compliance template.kioskapp/template.mobileapp.PolicyComplianceActivity AdminPin=12ab SystemUpdateWindow=bad ApplyPolicy=false
   ```

2. Status を開く → AdminPin と SystemUpdateWindow が `Error`、2 行目に「Admin PIN must be digits」「System update window must be HH:mm-HH:mm」
3. 構成を空に戻し、root を戻す → Status の AdminPin と SystemUpdateWindow が `Info` に戻る

   ```powershell
   & $adb -s emulator-5558 shell app_process -cp /data/local/tmp/kiosk-tools.dex /data/local/tmp Compliance template.kioskapp/template.mobileapp.PolicyComplianceActivity ApplyPolicy=false
   & $adb -s emulator-5558 shell rm /data/local/tmp/kiosk-tools.dex
   & $adb -s emulator-5558 unroot
   ```

- 誤った構成の間は PIN が `12ab` なので、保守の画面に入れない

### 後片付け

1. [後片付け](emulator.md#5-後片付け)(Device policy の Clear と Test configuration の Clear)
