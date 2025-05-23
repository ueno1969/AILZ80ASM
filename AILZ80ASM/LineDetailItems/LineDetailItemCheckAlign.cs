﻿using AILZ80ASM.AILight;
using AILZ80ASM.Assembler;
using AILZ80ASM.Exceptions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static AILZ80ASM.Assembler.Error;

namespace AILZ80ASM.LineDetailItems
{
    public class LineDetailItemCheckAlign : LineDetailItemCheck
    {
        private static readonly string RegexPatternStart_Arg1   = @"^(?<op1>(CHECK\s+ALIGN))\s+(?<arg1>[^,\s]+)$";
        private static readonly Regex CompiledRegexPatternStart_Arg1 = new Regex(
            RegexPatternStart_Arg1,
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase
        );
        private static readonly string RegexPatternStart_Arg1_2 = @"^(?<op1>(CHECK\s+ALIGN))\s+(?<arg1>[^,\s]+)\s*,\s*(?<arg2>[^,\s]*)$";
        private static readonly Regex CompiledRegexPatternStart_Arg1_2 = new Regex(
            RegexPatternStart_Arg1_2,
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase
        );
        private static readonly string RegexPatternEnd = @"^\s*ENDC\s*$";
        private static readonly Regex CompiledRegexPatternEnd = new Regex(
            RegexPatternEnd,
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase
        );
        public string AlignLabel { get; set; }
        public string DataLengthLabel { get; set; }

        private LineDetailItemCheckAlign(LineItem lineItem, AsmLoad asmLoad)
            : base(lineItem, asmLoad)
        {

        }

        private LineDetailItemCheckAlign(LineItem lineItem, string alignLabel, AsmLoad asmLoad)
            : this(lineItem, alignLabel, "1", asmLoad)
        {

        }

        private LineDetailItemCheckAlign(LineItem lineItem, string alignLabel, string dataLengthLabel, AsmLoad asmLoad)
            : base(lineItem, asmLoad)
        {
            AlignLabel = alignLabel;
            DataLengthLabel = dataLengthLabel;
        }

        public static LineDetailItemCheckAlign Create(LineItem lineItem, AsmLoad asmLoad)
        {
            if (!lineItem.IsCollectOperationString)
            {
                return default(LineDetailItemCheckAlign);
            }

            var startMatched_1   = CompiledRegexPatternStart_Arg1.Match(lineItem.OperationString);
            var startMatched_1_2 = CompiledRegexPatternStart_Arg1_2.Match(lineItem.OperationString);
            var endMatched = CompiledRegexPatternEnd.Match(lineItem.OperationString);

            if (endMatched.Success)
            {
                // 終了条件チェック
                if (asmLoad.Share.CheckLineDetailItemStack.Count == 0)
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E6001);
                }

                // 最終値を教える
                var lineDetailItemCheckAlign = new LineDetailItemCheckAlign(lineItem, asmLoad);
                var lineDetailItem = asmLoad.Share.CheckLineDetailItemStack.Pop();

                lineDetailItem.SetENDC(lineDetailItemCheckAlign);
                
                return lineDetailItemCheckAlign;
            }
            else
            {
                // 開始条件チェック
                if (startMatched_1.Success)
                {
                    var arg1 = startMatched_1.Groups["arg1"].Value;
                    var lineDetailItemCheckAlign = new LineDetailItemCheckAlign(lineItem, arg1, asmLoad);

                    asmLoad.AddValidateAssembles(lineDetailItemCheckAlign);
                    asmLoad.Share.CheckLineDetailItemStack.Push(lineDetailItemCheckAlign);

                    return lineDetailItemCheckAlign;
                }

                if (startMatched_1_2.Success)
                {
                    var arg1 = startMatched_1_2.Groups["arg1"].Value;
                    var arg2 = startMatched_1_2.Groups["arg2"].Value;
                    var lineDetailItemCheckAlign = new LineDetailItemCheckAlign(lineItem, arg1, arg2, asmLoad);

                    asmLoad.AddValidateAssembles(lineDetailItemCheckAlign);
                    asmLoad.Share.CheckLineDetailItemStack.Push(lineDetailItemCheckAlign);

                    return lineDetailItemCheckAlign;
                }
            }

            return default(LineDetailItemCheckAlign);
        }

        public override void ValidateAssemble()
        {
            //base.ValidateAssemble();
            var alignValue = AIMath.Calculation(AlignLabel).ConvertTo<UInt16>();
            var dataLength = AIMath.Calculation(DataLengthLabel).ConvertTo<UInt16>();

            if (alignValue <= 0 || (alignValue & (alignValue - 1)) != 0)
            {
                this.AsmLoad.AddError(new ErrorLineItem(this.LineItem, new ErrorAssembleException(Error.ErrorCodeEnum.E6013)));
                return;
            }

            // アドレスチェック
            var startAddress = this.Address.Value.Program;
            var endAddress = this.LineDetailItemCheck_ENDC.Address.Value.Program - dataLength;

            var maskAddress = ~(alignValue - 1);

            var maskedStartAddress = startAddress & maskAddress;
            var maskedEndAddress = endAddress & maskAddress;

            if (maskedStartAddress != maskedEndAddress)
            {
                // アライメント境界のアドレスを計算
                var alignStartAddress = ((int)(startAddress / alignValue)) * alignValue;
                var alignEndAddress = alignStartAddress + alignValue - 1;

                this.AsmLoad.AddError(new ErrorLineItem(this.LineItem, new ErrorAssembleException(Error.ErrorCodeEnum.E6012, alignStartAddress, alignEndAddress, startAddress, endAddress)));
            }
        }
    }
}
