# ✿ Flower Git

Git の簡易操作を行うエディタ拡張

## サポートする機能
- ワーキングツリーの操作
  - 任意のファイルをステージング（`git add {path}`）
  - 任意のファイルの変更取り消し（`git restore {path}`）
  - コンフリクト時は以下の取り込みが可能
    - _相手の変更_（`git checkout --theirs`）
    - _自分の変更_（`git checkout --ours`）
- ステージングの操作
  - ステージングの取り消し（`git restore --staged {path}`）
  - コミット（`git commit -m {message}`）
- リモートとの同期
  - プル（`pull --no-edit`）とプッシュ（`push`）を一括で行う

## サポートしない機能
- リポジトリ操作
- ブランチ操作
- その他チェックアウト、フェッチ、スタッシュなど

## 使い方
1. PackageManager に https://github.com/nenjiru/FlowerGit.git を追加
1. `Assets/Flower Git` で操作ウィンドウを開く
