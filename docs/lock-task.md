# ロックタスクと全画面

## 実現すること

- 許可されたアプリをロックタスクにし、HOME と履歴でほかへ移れないようにする(`startLockTask()`)
- デバイスオーナーは自分と構成のほかのアプリを許可し(`setLockTaskPackages()`)、許可されていれば起動で自動で入る(`LockTaskMode = "if_whitelisted"`)
- ロックタスクの間は、構成に従ってシステムバーを隠し(全画面)、画面を点けたままにする(`HideSystemBars`・`KeepScreenOn`)
- 許可が無い時(デバイスオーナーでも MDM でもない時)は画面の固定になり、利用者の確認が要る

## 確かめ方

前提: [エミュレーターの準備](emulator.md) を済ませる。

### ロックタスク

1. Lock task を開く → State が `None`
2. Start を押す → `Locked`。システムバーが消えて全画面になる(上からのスワイプで一時的に出る)
3. ツールバーの HOME(○)と履歴(□)を押す → 画面は変わらない
4. Stop を押す → `None`。システムバーが戻る

### 画面の固定(デバイスオーナーでない時)

1. Device policy が `Not applied` かを見る(`Applied` なら Clear を押す)
2. デバイスオーナーを外す → `Success: Admin removed ...`

   ```powershell
   & $adb -s emulator-5558 shell dpm remove-active-admin template.kioskapp/template.mobileapp.KioskAdminReceiver
   ```

3. アプリを開き直す(デバイスオーナーでないので force-stop が効く)

   ```powershell
   & $adb -s emulator-5558 shell am force-stop template.kioskapp
   & $adb -s emulator-5558 shell am start -n template.kioskapp/template.mobileapp.MainActivity
   ```

4. Status を開く → Device owner が `No`、App feedback が `None`
5. Menu を見る → Device policy・App update・Test configuration・Reboot が灰色で押せない
6. Lock task を開き、Start を押す → 画面の固定の確認(App is pinned)が出る
7. 30 秒以上待ってから Got it を押す(続けて「Viewing full screen」が出たら Got it で閉じる)→ State がすぐ(1 秒ほどで)`Pinned` になり、Stop を押せる
8. Stop を押す → `None`

### 後片付け

1. デバイスオーナーを付け直す → `Success: Device owner set to package ...`

   ```powershell
   & $adb -s emulator-5558 shell dpm set-device-owner template.kioskapp/template.mobileapp.KioskAdminReceiver
   ```

2. [止めて開き直す](emulator.md#止める) → Status の Device owner が `Yes`、App feedback に 4 つの状態。Menu の 4 つのボタンも押せる
