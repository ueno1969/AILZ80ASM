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
- IXH, IXL, IYH, IYLに対応

## ラベル
- ネームスペースに対応：ネームスペース無指定時はファイル名がネームスペース名として設定
- <ラベル名[:]> 標準ラベル
- <[.]ローカルラベル名> ローカルラベル

#### ラベルの指定
- <ネームスペース>.<ラベル名>.<ローカルラベル名>
- <ラベル名>.<ローカルラベル名>
- <ローカルラベル名>

#### ラベルの機能
- .@Hをラベルに追加すると、上位1バイトを取得
- .@Lをラベルに追加すると、下位1バイトを取得

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
- ; 以降はコメントとして処理されます

## 制御命令
#### ORG <式>, [<式2>]
- ロケーションカウンタの値を、<式>の値に変更します
- <式2>に、出力アドレスを設定します
#### INCLUDE [<ファイル名>]
- ファイル名の内容を、その場所に展開します
#### DB <式>, [<式>]
- <式>の1バイト値を設定します
#### DB [<変数名>=<ループ開始値>..<ループ終了値>:<式>]
- ループの条件で、式の内容を展開します
- ネストも可能
- 例：DB [Y=0..2:[X=0..4:Y*8+X]]
#### DW <式>, [<式>]
- <式>の2バイト値を設定します
#### DW [<変数名>=<ループ開始値>..<ループ終了値>:<式>]
- ループの条件で、式の内容を展開します
- ネストも可能
- 例：DW [Y=24..0:$8000 + Y * $140]
#### DS <式>, [<式2>] 及び DBS <式>, [<式2>]
- <式>のバイト数、0で埋めます。<式2>を設定するとその値で埋めます
#### DWS <式>, [<式2>]
- <式>の２バイト数、0で埋めます。<式2>を設定するとその値で埋めます

## マクロ
#### MACRO <マクロ名> [<引数1>, <引数2>]　～ END MACRO
- MACROからEND MACROまでがマクロとして定義されます
- 引数に付けた名前がマクロ内で利用できます

#### REPEAT <式1> [LAST <式2>]　～ END REPEAT
- 式1に設定した値の回数分をREPEATの中に記述してある命令を展開します
- 式2には、負の値を指定します。最終の展開時に削除したい命令数を指定します

