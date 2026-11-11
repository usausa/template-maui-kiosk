# ステータスバーの無効とアプリを隠す

## 実現すること

- Apply でステータスバーを無効にし(`setStatusBarDisabled()`)、ロックタスクの外(保守の間やアプリの更新の間)でも通知の欄とクイック設定を開けなくする
- Apply で、ランチャーとホームのアプリのうち、このアプリ・構成のほかのアプリ(`LockTaskPackages`)・設定を除いたものを隠す(`setApplicationHidden()`)
- 隠したアプリはデータが残り、Clear で戻る。方針を掛けている間に構成のほかのアプリが変わると、隠し直す
- ほかのホームアプリが無くなるので、アプリの更新の間も標準のランチャーは出ない

## 確かめ方

前提: [エミュレーターの準備](emulator.md) を済ませる。

### Apply

1. 端末のパッケージの数を見る(エミュレーターのイメージによる。android-35 の google_apis では `242`)

   ```powershell
   (& $adb -s emulator-5558 shell pm list packages).Count
   ```

2. Device policy の Apply を押す → `Applied`
3. 1. の数をもう一度見る → 減る(`224`)
4. ホームの候補を見る → このアプリと設定だけ

   ```powershell
   & $adb -s emulator-5558 shell cmd package query-activities -a android.intent.action.MAIN -c android.intent.category.HOME | Select-String "packageName="
   ```

### 構成のほかのアプリ

1. Test configuration の Lock task apps に `com.google.android.deskclock` を入れて Send → 1. の数が 1 つ増える
2. Menu の Apps を開く → Clock が押せて、時計のアプリが開く。ツールバーの戻る(◁)でアプリに戻る

### ステータスバー

1. Lock task の Stop を押す(ロックタスクを外す)→ `None`
2. 画面の上から下へ 2 回スワイプする → 通知の欄とクイック設定は出ない(時刻とアイコンが一時的に出るだけ)
3. Device policy の Clear を押す → 1. の数が始めに戻る
4. 数秒待ってから、上から下へスワイプする → 通知の欄が出る(ツールバーの戻る(◁)で閉じる)

- Clear の直後は隠したアプリを戻している間で、スワイプが効かないことがある

### 後片付け

1. Test configuration の Clear を押す
