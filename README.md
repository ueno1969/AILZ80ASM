# Z80 Assembler 'AILZ80ASM'
AILZ80ASMは、C#で書かれた.NET Coreの環境で動作するZ80アセンブラです。

`本プログラムは、開発評価版です。不具合が取り切れておりませんので、ご利用には十分注意してください。`

## 使い方
AILZ80ASM [<オプション>] <オプション指定文字列:ファイル名等> [, <オプション指定文字列:ファイル名等>]

## コマンドラインオプション
--input -i : アセンブリ対象のファイル（複数指定する場合には、カンマ区切り）

--output -o  : 出力ファイル

# ソースコード書式

## ニーモニック
- ザイログ・ニーモニックの表記
- 大文字、小文字の区別はしない

## 未定義命令 (Undocumented Instructions) 
- 未対応、今後対応予定

## ラベル
- ネームスペースに対応：ネームスペース無指定時はファイル名がネームスペース名として設定
- <ラベル名[:]> 標準ラベル
- <[.]ローカルラベル名> ローカルラベル

#### ラベルの指定
- <ネームスペース>.<ラベル名>.<ローカルラベル名>
- <ラベル名>.<ローカルラベル名>
- <ローカルラベル名>

#### ラベル指定サンプル
```
	org $8000
	ld a, 0
addr:
	ld a, (addr)
	ld hl, addr
.test
	ld a,(addr.test)
	ld hl, addr.test
	ld hl, (addr.test)
```

## 即値
- 2進数、10進数、16進数
- 2進数：末尾に%を付けます
- 10進数：何もつけません
- 16進数：先頭に$もしくは末尾にHを付けます

## ロケーションカウンタ
- $ は、現在のロケーションカウンタを参照することができます

## 式
- C#に従います

## コメント
- ; 以降はコメントと処理されます

## 制御命令
#### ORG [<式>]
- ロケーションカウンタの値を、<式>の値に変更します
#### INCLUDE [<ファイル名>]
- 未実装、将来開発予定
#### DB <式>, [<式>]
- <式>の1バイト値を設定します
#### DW <式>, [<式>]
- <式>の2バイト値を設定します

## マクロ
- 未実装、将来開発予定