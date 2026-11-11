# 権限の自動の許可

## 実現すること

- Apply で、端末のアプリが求める実行時の権限を、ダイアログを出さずに許可する(`setPermissionPolicy()` の `PERMISSION_POLICY_AUTO_GRANT`)
- デバイスオーナーなので、カメラ・位置・マイクなどの権限も許可できる
- Clear で確認する方針(`PERMISSION_POLICY_PROMPT`)に戻す。それまでに許可した権限は、許可のまま管理者の固定(`POLICY_FIXED`)で残る(利用者は設定で変えられない)

## 確かめ方

前提: [エミュレーターの準備](emulator.md) と [試しのプログラム](emulator.md#6-試しのプログラム) を済ませる。
エミュレーターのカメラのアプリが、最初の起動で位置の権限を求めるのを使う。

### 許可

1. Test configuration の Lock task apps に `com.android.camera2` を入れて Send し、Device policy の Apply を押す
2. Menu の Apps を開き、Camera を押す → 最初の「Remember photo locations?」で NEXT を押すと、位置の権限のダイアログが出ずにカメラの画面になる
3. 位置の権限を見る → `ACCESS_FINE_LOCATION` と `ACCESS_COARSE_LOCATION` が `granted=true` で `POLICY_FIXED`

   ```powershell
   & $adb -s emulator-5558 shell dumpsys package com.android.camera2 | Select-String "LOCATION: granted"
   ```

4. ツールバーの戻る(◁)でアプリに戻り、Device policy の Clear を押す → 3. は `granted=true` と `POLICY_FIXED` のまま。通知の欄に「Camera has location access」

### 後片付け

カメラの権限の固定の印を root で外し、カメラを最初の状態に戻す。

```powershell
& $adb -s emulator-5558 root
& $adb -s emulator-5558 wait-for-device
& $adb -s emulator-5558 push __Emulator\kiosk-tools.dex /data/local/tmp/kiosk-tools.dex
& $adb -s emulator-5558 shell app_process -cp /data/local/tmp/kiosk-tools.dex /data/local/tmp ClearPolicyFixed com.android.camera2 android.permission.ACCESS_FINE_LOCATION
& $adb -s emulator-5558 shell app_process -cp /data/local/tmp/kiosk-tools.dex /data/local/tmp ClearPolicyFixed com.android.camera2 android.permission.ACCESS_COARSE_LOCATION
& $adb -s emulator-5558 shell pm revoke com.android.camera2 android.permission.ACCESS_FINE_LOCATION
& $adb -s emulator-5558 shell pm revoke com.android.camera2 android.permission.ACCESS_COARSE_LOCATION
& $adb -s emulator-5558 shell pm clear com.android.camera2
& $adb -s emulator-5558 shell rm /data/local/tmp/kiosk-tools.dex
& $adb -s emulator-5558 unroot
```

- `pm clear-permission-flags` では `POLICY_FIXED` を外せない
- 続けて [後片付け](emulator.md#5-後片付け)(Test configuration の Clear)
