# Device Policy とは

## Device Policy

- Android の Device Policy(端末の方針)は、端末の管理者のアプリが、端末の設定や使える機能を決める仕組み
- 管理者のアプリは Device Policy Controller(DPC)と呼ばれ、`DeviceAdminReceiver` を持ち、`DevicePolicyManager` の API で方針を掛ける
- 方針の例: 使えるアプリを決めた物だけにする(ロックタスク)、初期化やセーフモードの禁止、ロック画面の無効、システムの更新の時間帯、ステータスバーの無効、アプリを隠す、権限の自動の許可
- どこまで掛けられるかは、管理者の種類で決まる

## 管理者の種類

| 種類 | 管理する範囲 | このサンプル |
|---|---|---|
| デバイスオーナー | 端末全体(会社が持つ専用の端末) | 使う |
| プロファイルオーナー | 仕事用プロファイルだけ(私物の端末) | 使わない |

- デバイスオーナーは端末に 1 つだけで、初期化した端末のセットアップ(QR コードなど)で決まる
- 試しの端末では、アカウントが無ければ `adb shell dpm set-device-owner` でもなれる
- キオスクやサイネージのような専用の端末の API(ロックタスクなど)は、デバイスオーナーが使う

## MDM との関係

- MDM(EMM)は、管理のサーバーと画面から、端末の DPC を通して方針を配る
- MDM はアプリに値を配り(管理対象の構成)、アプリは状態を MDM に返す(Keyed App States)

## このサンプルの使い方

| 使い方 | デバイスオーナー | このアプリの役割 |
|---|---|---|
| MDM から使う | MDM の DPC | 管理されるキオスクのアプリ。構成を読み、ロックタスクに入り、状態を報告する |
| 簡易 MDM | このアプリ | 自分が DPC になり、Device policy の Apply で専用の端末の方針をまとめて掛ける |

- 簡易 MDM の時は、MDM の代わりに Test configuration で自分に構成を配って試す
- 手順書の確かめは、簡易 MDM の使い方で行う

## 手順書

| 手順書 | 内容 |
|---|---|
| [emulator.md](emulator.md) | エミュレーターの準備(確かめの前に行う) |
| [managed-configuration.md](managed-configuration.md) | 管理対象の構成 |
| [lock-task.md](lock-task.md) | ロックタスク・全画面・画面の固定 |
| [device-policy.md](device-policy.md) | 端末の制限・ホームアプリ・再起動 |
| [status-bar-and-hidden-apps.md](status-bar-and-hidden-apps.md) | ステータスバーの無効とアプリを隠す |
| [permissions.md](permissions.md) | 権限の自動の許可 |
| [other-apps.md](other-apps.md) | ロックタスクの間のほかのアプリ |
| [system-update.md](system-update.md) | システムの更新の時間帯 |
| [app-feedback.md](app-feedback.md) | MDM への状態の報告 |
| [lock-screen-messages.md](lock-screen-messages.md) | ロック画面とサポートの文 |
| [admin-pin.md](admin-pin.md) | 保守の画面と PIN |
| [qr-provisioning.md](qr-provisioning.md) | QR コードでのデバイスオーナーの設定 |
| [app-update.md](app-update.md) | アプリ自身の更新 |

## 参考

- [Android Enterprise](https://developer.android.com/work)
- [Dedicated devices overview](https://developer.android.com/work/dpc/dedicated-devices)
- [Build a device policy controller](https://developer.android.com/work/dpc/build-dpc)
- [DevicePolicyManager](https://developer.android.com/reference/android/app/admin/DevicePolicyManager)
