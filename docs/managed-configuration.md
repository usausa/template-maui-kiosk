# 管理対象の構成

## 実現すること

- MDM が配る値(管理対象の構成)をアプリが読み、配られていないキーは既定値で動く
- 構成が変わると、その場で読み直して画面と動きに反映する
- 項目は `app_restrictions.xml` で宣言し、`RestrictionsManager` で読む
- キーと既定値は README の「管理対象の構成」の表

## 確かめ方

前提: [エミュレーターの準備](emulator.md) を済ませる。
このサンプルでは、デバイスオーナーのアプリが自分に構成を配る Test configuration で、MDM の代わりにする。

### 値を配る

1. Status を開く → Managed configuration の Admin PIN・Keep screen on・Hide system bars・Update window・Lock task apps が `Default`、Message が `-`
2. Test configuration を開き、Admin PIN に `1234`、Message に `Hello kiosk` を入れて Send →「Configuration sent」
3. Back で Menu に戻る → 上に `Hello kiosk`
4. Status を開く → Admin PIN・Keep screen on・Hide system bars が `MDM`、Message が `Hello kiosk`
5. Lock task を PIN `0000` で開こうとする →「PIN is incorrect」。PIN `1234` で開ける

### 既定値に戻す

1. Test configuration を PIN `1234` で開き、Clear を押す →「Configuration cleared」
2. Status を開く → 1. と同じく `Default`。Menu の知らせは消え、PIN は `0000` に戻る
