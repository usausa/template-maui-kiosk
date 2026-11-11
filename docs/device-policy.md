# 端末の制限

## 実現すること

- Device Policy(端末の方針)は、端末の管理者のアプリが `DevicePolicyManager` で端末の設定や使える機能を決める仕組み(詳しくは [Device Policy とは](overview.md))
- Device policy の画面の Apply で、このアプリがデバイスオーナーとして専用の端末の方針をまとめて掛け、Clear で外す
- このアプリをホームに固定し(`addPersistentPreferredActivity()`)、再起動の後もロックタスクで立ち上がる
- ロックタスクの機能は時刻などの表示と電源メニューだけにし(`setLockTaskFeatures()`)、HOME と履歴ではアプリから出られない
- ロック画面の無効(`setKeyguardDisabled()`)、充電中の点灯(`STAY_ON_WHILE_PLUGGED_IN`)、セーフモード・初期化・ユーザーの追加・外部メディアの禁止(`addUserRestriction()`)
- 一緒に掛けるもの: [システムの更新の時間帯](system-update.md)・[ステータスバーの無効とアプリを隠す](status-bar-and-hidden-apps.md)・[権限の自動の許可](permissions.md)
- Menu の Reboot で端末を再起動する(`reboot()`)

## 確かめ方

前提: [エミュレーターの準備](emulator.md) を済ませる。

### Apply

1. Device policy を開く → State が `Not applied`
2. Apply を押す →「Policy applied」、State が `Applied`。ロックタスク(全画面)に入る
3. ツールバーの HOME(○)と履歴(□)を押す → このアプリから出ない

### 再起動

1. Menu の Reboot を押す → PIN `0000` の後に「Reboot the device?」→ OK で端末が再起動する
2. 起動の後 → ロック画面を経ずに、このアプリがロックタスク(全画面)で立ち上がる

### Clear

1. Device policy の Clear を押す →「Policy cleared」、State が `Not applied`。ロックタスクが外れる(隠したアプリを戻すので、数秒から十数秒かかる)
2. 「Pixel Launcher keeps stopping」が出たら Close app で閉じる(方針を掛けたまま再起動した後の Clear では、エミュレーターの標準のランチャーが落ち続ける)
3. Menu の Reboot で再起動する → 起動の後に標準のロック画面が出る
4. ロック画面を上へスワイプする → 標準のランチャーが出る
5. アプリの一覧の KioskApp でアプリに戻る
