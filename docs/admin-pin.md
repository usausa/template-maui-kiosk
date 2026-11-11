# 保守の画面と PIN

## 実現すること

- Menu の保守の画面(Lock task・Device policy・App update・Test configuration)と Reboot は、押すたびに PIN を求める
- PIN は構成の `AdminPin`(既定は `0000`)。デバイスオーナーでない時は、Lock task 以外のボタンを押せない
- PIN を 5 回続けて誤ると 30 秒待たせ、待ちの間は PIN の入力を出さない(待ちの後も 5 回ごと)。正しい PIN で数を 0 に戻す
- 誤りの数と待ちの終わりの時刻はアプリの設定に残し、アプリを開き直しても続く。待ちに入るとログに残す

## 確かめ方

前提: [エミュレーターの準備](emulator.md) を済ませる。

### PIN

1. Menu の Lock task・Device policy・App update・Test configuration を順に押す → どれも PIN `0000` で開き、Back で Menu に戻る
2. Reboot を押す → PIN の後に「Reboot the device?」(Cancel で閉じる)

### 誤りの待ち

1. Lock task で誤った PIN(`1111`)を 4 回入れる → 毎回「PIN is incorrect」
2. 5 回目は Device policy で誤った PIN を入れる(数はボタンをまたいで続く)→「Try again in 30 seconds」
3. すぐに Reboot を押す → PIN の入力は出ず「Try again in N seconds」(N は残りの秒)
4. [止めて開き直し](emulator.md#止める)、Lock task を押す → 待ちが続いていて「Try again in N seconds」
5. 30 秒たってから正しい PIN(`0000`)を入れる → 画面が開く。Back で戻り、誤った PIN を入れる →「PIN is incorrect」(数は 0 から)
6. (任意)ログを見る → `Admin PIN locked. failures=[5], until=[…]`

   ```powershell
   & $adb -s emulator-5558 logcat -d | Select-String "Admin PIN locked"
   ```

- 3. と 4. は、2. の後の 30 秒のうちに行う
- 最後に正しい PIN で一度開き、誤りの数を 0 に戻しておく
