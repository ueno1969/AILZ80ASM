﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AILZ80ASM.Assembler
{
    public class AsmEnum
    {
        /// <summary>
        /// ファイルタイプ
        /// </summary>
        public enum FileTypeEnum
        {
            Z80,
            BIN,
            HEX,
            T88,
            CMT,
            MZT,
            LST,
            SYM,
            EQU,
            ADR,
            ERR,
            DBG,
            TAG,
        }

        /// <summary>
        /// ファイルのデータタイプ
        /// </summary>
        public enum FileDataTypeEnum
        {
            Binary,
            Text,
        }

        /// <summary>
        /// リスティングファイルの形式
        /// </summary>
        public enum ListFormatEnum
        {
            Simple,
            Middle,
            Full,
        }

        /// <summary>
        /// シンボルファイルの形式
        /// </summary>
        public enum SymbolFormatEnum
        {
            Minimal_Equ,
            Normal,
        }

        /// <summary>
        /// エンコードのモード
        /// </summary>
        public enum EncodeModeEnum
        {
            AUTO,
            UTF_8,
            SHIFT_JIS,
        }

        /// <summary>
        /// エンコードのモード
        /// </summary>
        internal enum InternalEncodeModeEnum
        {
            ASCII,
            UTF_8,
            SHIFT_JIS,
        }
    }
}
