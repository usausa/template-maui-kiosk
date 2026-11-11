# システムの更新の時間帯

## 実現すること

- Apply で、自動のシステムの更新を毎日の時間帯に限る(`setSystemUpdatePolicy()` の `createWindowedInstallPolicy()`)
- 時間帯は構成の `SystemUpdateWindow`(`HH:mm-HH:mm`、既定は `02:00-04:00`)。開始が終わりより後なら日付をまたぐ
- 方針を掛けている間に構成が変わると、新しい時間帯で掛け直す。Clear で外す
- Status の System update に端末に掛かっている方針、Update window に構成の値を出す

## 確かめ方

前提: [エミュレーターの準備](emulator.md) を済ませる。

### 既定の時間帯

1. Device policy の Apply を押す → `Applied`
2. Status を開く → System update が `02:00-04:00`、Update window が `02:00-04:00`(`Default`)
3. (任意)端末に掛かった方針を見る → `windowStart: 120, windowEnd: 240`

   ```powershell
   & $adb -s emulator-5558 shell dumpsys device_policy | Select-String "System Update Policy"
   ```

### 構成の時間帯

1. Test configuration の Update window に `03:30-05:00` を入れて Send →「Configuration sent」
2. Status を開く → System update と Update window が `03:30-05:00`、Update window が `MDM`
3. Test configuration の Update window に `3:00-5:00` を入れて Send →「Update window must be HH:mm-HH:mm」(OK で閉じる)
4. Test configuration の Clear を押し、Status を開く → System update と Update window が `02:00-04:00`(`Default`)に戻る

### 外す

1. Device policy の Clear を押し、Status を開く → System update が `None`
