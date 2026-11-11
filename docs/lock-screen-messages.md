# ロック画面とサポートの文

## 実現すること

- 構成の `LockScreenInfo` をロック画面に出す(`setDeviceOwnerLockScreenInfo()`)
- 構成の `SupportMessage` を、制限で止めた操作の画面(設定の初期化など)に出す(`setShortSupportMessage()`)
- デバイスオーナーが構成を配る時に、方針の Apply とは別に設定し、キーが無ければ外す
- 方針を掛けている間はロック画面が出ない(ロック画面の無効)ので、ロック画面の文は Clear の後に見える

## 確かめ方

前提: [エミュレーターの準備](emulator.md) を済ませる。

### サポートの文

1. Test configuration の Lock screen info に `Kiosk device. Ask the staff for help.`、Support message に `Blocked on this kiosk. Ask the staff.` を入れて Send → Status の Lock screen info と Support message に文が出る
2. Device policy の Apply を押す。Lock task の Stop を押してから Open settings を押す
3. 設定の System → Reset options → Erase all data (factory reset)(灰色)を押す →「Blocked by work policy」の画面に `Blocked on this kiosk. Ask the staff.`。Close で閉じる
4. ツールバーの HOME(○)でアプリに戻り、Device policy の Clear を押す

### ロック画面の文

1. ツールバーの電源ボタンで画面を消し、もう一度押して点ける → ロック画面の下に `Kiosk device. Ask the staff for help.`(「This device belongs to your organization」と交互に出る)
2. 上へスワイプしてアプリに戻る
3. Test configuration の Clear を押し、1. をもう一度 → ロック画面に文が出ない

- ロック画面は約 10 秒で眠るので、見る時は電源ボタンで点け直す
