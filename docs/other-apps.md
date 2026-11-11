# ほかのアプリ

## 実現すること

- 構成の `LockTaskPackages`(パッケージ名のカンマ区切り)のアプリを、このアプリと一緒にロックタスクで許可する(`setLockTaskPackages()`)
- Menu の Apps に許可したアプリのボタンを並べ、押すと開く。入っていないアプリは押せない
- ほかのアプリでも履歴は開かず、HOME はこのアプリ(ホーム)に戻る。戻るキーでもこのアプリに戻る
- 許可が掛かっている間に構成が変わると、許可を掛け直す

## 確かめ方

前提: [エミュレーターの準備](emulator.md) を済ませる。

### 許可するアプリ

1. Menu の Apps を開く → `No apps`
2. Test configuration の Lock task apps に `com.google.android.deskclock,com.google.android.calendar,com.example.none` を入れて Send
3. Status を開く → Lock task apps が `3`(`MDM`)
4. Apps を開く → `Clock`・`Calendar` のボタンと、灰色で押せない `com.example.none`

### ロックタスクの中で開く

1. Device policy の Apply を押す → `Applied`(ロックタスクに入る)
2. Apps を開き、Clock を押す → 時計のアプリが開く
3. ツールバーの履歴(□)を押す → 何も開かず、時計のまま
4. ツールバーの HOME(○)を押す → このアプリ(ホーム)に戻る。ロックタスクのまま
5. もう一度 Clock を開き、ツールバーの戻る(◁)を押す → このアプリの Apps に戻る
6. Calendar を押す → カレンダーが開く(最初は案内の画面)。戻る(◁)で Apps に戻る

### 掛け直し

1. Test configuration の Lock task apps を `com.google.android.deskclock` だけにして Send
2. Apps を開く → Clock だけ

### 後片付け

1. [後片付け](emulator.md#5-後片付け)(Device policy の Clear と Test configuration の Clear)
