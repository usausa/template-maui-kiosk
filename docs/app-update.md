# 自分自身の更新

## 実現すること

- App update の Update で、構成の `UpdateLocation` の APK を落とし、このアプリで versionCode が大きい時だけ入れる(`PackageInstaller` のセッション)
- デバイスオーナーなので確認なしで入り、入れ終えると `ACTION_MY_PACKAGE_REPLACED` でロックタスクのアプリを開き直す
- 待ち表示(「Downloading...」→「Updating...」)はアプリが止まるまで出し続け、操作を受けない。インストールに失敗すると「Update failed」
- 方針を掛けている間は、ほかのホームアプリを隠しステータスバーを無効にしているので、インストールの間にほかのものは開かない
- 同じか古い版は「The app is up to date」、取れない時は「Download failed」

## 確かめ方

前提: [QR でのデバイスオーナーの設定](qr-provisioning.md) を済ませ、QR 用の AVD を動かしたままにしておく(Release の版がデバイスオーナーで、方針を掛けた状態)。
手元の HTTP で配るので、確かめの版だけ平文の HTTP を許して作る(スクリプトが manifest に一時的に足し、終わったら戻す)。

### 準備

1. QR の設定の HTTP のサーバーが動いていれば Ctrl+C で止める
2. 確かめの版を作る → `__Emulator\update\v1\` に 1.0 (1)、`v2\` に 1.1 (2)、`other\` に別の鍵で署名した 1.1 (2)

   ```powershell
   .\tools\emulator\build-update-apks.ps1
   ```

3. 版 1.0 を入れる → アプリが開き直り、ロックタスクのまま

   ```powershell
   & $adb -s emulator-5560 install -r -d __Emulator\update\v1\template.kioskapp-Signed.apk
   ```

4. 別の PowerShell で 1.1 を配り、端末から届くようにする

   ```powershell
   .\tools\emulator\serve.ps1 -Root __Emulator\update\v2
   ```

   ```powershell
   & $adb -s emulator-5560 reverse tcp:18080 tcp:18080
   ```

### 更新

1. App update を PIN `1234` で開く → Version が `1.0 (1)`、Update は押せない(URL が無い)
2. Test configuration の Update URL に `http://127.0.0.1:18080/template.kioskapp-Signed.apk` を入れて Send → App update の Update が押せる
3. Update を押し、「Update the app?」で OK →「Downloading...」の後に「Updating...」(この間は戻る・タップ・HOME が効かない)。アプリの起動画面だけが数秒出て(標準のランチャーは出ず、上からスワイプしても通知の欄は出ない)、Menu で開き直る
4. App update を開く → Version が `1.1 (2)`。ロックタスクと方針はそのまま
5. もう一度 Update を押す →「The app is up to date」
6. Test configuration の Update URL を `http://127.0.0.1:18080/none.apk` にして Send し、App update の Update を押す →「Download failed」

### 失敗

1. 版 1.0 を入れ直す

   ```powershell
   & $adb -s emulator-5560 install -r -d __Emulator\update\v1\template.kioskapp-Signed.apk
   ```

2. 配っている HTTP のサーバーを Ctrl+C で止め、別の鍵の版を配る

   ```powershell
   .\tools\emulator\serve.ps1 -Root __Emulator\update\other
   ```

3. Test configuration の Update URL を `http://127.0.0.1:18080/template.kioskapp-Signed.apk` に戻して Send し、App update の Update を押す →「Downloading...」「Updating...」の後に「Update failed」。アプリは閉じず、Version は `1.0 (1)` のまま
4. (任意)ログを見る → 更新できた時は `App update status. status=[0], message=[INSTALL_SUCCEEDED: Session installed]`、失敗の時は `status=[5], message=[INSTALL_FAILED_UPDATE_INCOMPATIBLE: ...]`

   ```powershell
   & $adb -s emulator-5560 logcat -d | Select-String "App update"
   ```

### 後片付け

1. HTTP のサーバーを Ctrl+C で止め、転送を外してエミュレーターを閉じる

   ```powershell
   & $adb -s emulator-5560 reverse --remove-all
   ```

- 本番の `UpdateLocation` は HTTPS にする。配る APK は作り直して(`--no-incremental`)作り、試しの端末で起動を確かめてから置く
