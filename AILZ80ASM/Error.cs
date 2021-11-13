﻿using System.Collections.Generic;

namespace AILZ80ASM
{
    public static class Error
    {
        public enum ErrorTypeEnum
        {
            Error,
            Warning,
            Infomation,
        }

        public enum ErrorCodeEnum
        {
            E0001,
            E0002,
            E0003,
            E0004,
            E0005,
            E0006,
            E0007,
            E0008,
            E0009,
            E0012,
            E0013,
            E0014,
            E0015,
            E0016,

            E1011,
            E1012,
            E1013,
            E1014,
            E1015,

            E1021,
            E1022,
            E1023,
            E1024,

            E1031,
            E1032,

            E2001,
            E2002,
            E2003,
            E2004,
            E2005,
            E2006,
            E2007,
            // マクロ
            E3001,
            E3002,
            E3003,
            E3004,
            E3005,
            E3006,
            E3007,
            E3008,
            E3009,
            E3010,

            W0001,
            I0001,
        }

        public static ErrorTypeEnum GetErrorType(ErrorCodeEnum errorCode)
        {
            if (errorCode < ErrorCodeEnum.W0001)
            {
                return ErrorTypeEnum.Error;
            }
            else if (errorCode < ErrorCodeEnum.I0001)
            {
                return ErrorTypeEnum.Warning;
            }
            else
            {
                return ErrorTypeEnum.Infomation;
            }
        }

        public static string GetMessage(ErrorCodeEnum errorCode)
        {
            return GetMessage(errorCode, default);
        }

        public static string GetMessage(ErrorCodeEnum errorCode, params object[] parameters)
        {
            var message = ErrorMessages[errorCode];
            return parameters == default ? message : string.Format(message, parameters);
        }

        private static readonly Dictionary<ErrorCodeEnum, string> ErrorMessages = new Dictionary<ErrorCodeEnum, string>()
        {
            [ErrorCodeEnum.E0001] = "無効な命令が指定されました。",
            [ErrorCodeEnum.E0002] = "バイト変換で有効桁数をオーバーしました。",
            [ErrorCodeEnum.E0003] = $"相対ジャンプの範囲違反、有効範囲は{byte.MinValue}～{byte.MaxValue}までです。",
            [ErrorCodeEnum.E0004] = "演算、もしくはラベルの解決に失敗しました。定義を確認してください。{0}",
            [ErrorCodeEnum.E0005] = "16進数の変換に失敗しました。",
            [ErrorCodeEnum.E0006] = "10進数の変換に失敗しました。",
            [ErrorCodeEnum.E0007] = "8進数の変換に失敗しました。",
            [ErrorCodeEnum.E0008] = "2進数の変換に失敗しました。",
            [ErrorCodeEnum.E0009] = "ORGに指定した出力アドレス上に既にアセンブリ結果があります。",
            [ErrorCodeEnum.E0012] = "データの指定が間違っています。",
            [ErrorCodeEnum.E0013] = "ラベルの指定が間違っています。",
            [ErrorCodeEnum.E0014] = "同名のラベルが既に指定されています。",
            [ErrorCodeEnum.E0015] = "ALIGNに指定したアドレスは、2のべき乗である必要があります。",
            [ErrorCodeEnum.E0016] = "指定できる値は、0～7です。{0}",

            // リピート
            [ErrorCodeEnum.E1011] = "REPEATに対応するEND REPEATが見つかりませんでした。",
            [ErrorCodeEnum.E1012] = "END REPEATが先に見つかりました。",
            [ErrorCodeEnum.E1013] = "LASTに指定した値が不正です。",
            [ErrorCodeEnum.E1014] = "REPEATでは、ローカルラベルしか使えません。",
            [ErrorCodeEnum.E1015] = "REPEATに指定した値が不正です。",

            // コンディショナル
            [ErrorCodeEnum.E1021] = "#IFに対応する#ENDIFが見つかりませんでした。",
            [ErrorCodeEnum.E1022] = "#ENDIFが先に見つかりました。",
            [ErrorCodeEnum.E1023] = "#ELSEの後に#ELSIF、#ELSEは設定できません。",
            [ErrorCodeEnum.E1024] = "#IF、#ELSIF、#ELSE、#ENDIFにラベルは設定できません。",

            // エラー
            [ErrorCodeEnum.E1031] = "#ERROR:{0}",
            [ErrorCodeEnum.E1032] = "#ERRORにラベルは設定できません。",

            // Include
            [ErrorCodeEnum.E2001] = "Include 展開先でエラーが発生しました。",
            [ErrorCodeEnum.E2002] = "Include ファイルが存在しませんでした。[{0}]",
            [ErrorCodeEnum.E2003] = "Include 既に読み込み済みのファイルです。[{0}]",
            [ErrorCodeEnum.E2004] = "Include 開始アドレスの指定が間違っています。",
            [ErrorCodeEnum.E2005] = "Include 長さの指定が間違っています。",
            [ErrorCodeEnum.E2006] = "Include 開始アドレスがファイルの長さを超えています。",
            [ErrorCodeEnum.E2007] = "Include にはラベルを指定できません。",
            // マクロ
            [ErrorCodeEnum.E3001] = "MACROに対応するEND MACROが見つかりませんでした。",
            [ErrorCodeEnum.E3002] = "END MACROが先に見つかりました。",
            [ErrorCodeEnum.E3003] = "MACROが重複登録されていますので、名前解決が出来ません。",
            [ErrorCodeEnum.E3004] = "MACROの引数の数が一致していません。",
            [ErrorCodeEnum.E3005] = "MACROの引数名が有効ではありません。{0}",
            [ErrorCodeEnum.E3006] = "MACROでは、ローカルラベル以外は使えません。",
            [ErrorCodeEnum.E3007] = "MACROの名前が有効ではありません。",
            [ErrorCodeEnum.E3008] = "MACROの中から自分自身のMACROを呼び出すことは出来ません。",
            [ErrorCodeEnum.E3009] = "MACROが見つかりませんでした。ネームスペース付きの名前を利用すると解決する場合があります。{0}",
            [ErrorCodeEnum.E3010] = "同名のマクロが既に指定されています。",

            [ErrorCodeEnum.W0001] = "1バイトの指定場所に、{0}が設定されています。1バイトに丸められます。",


            [ErrorCodeEnum.I0001] = "未定義"
        };
    }
}
