﻿using AILZ80ASM.AILight;
using AILZ80ASM.Assembler;
using AILZ80ASM.Exceptions;
using System;
using System.IO;
using System.Text.RegularExpressions;

namespace AILZ80ASM.LineDetailItems
{
    public class LineDetailItemEndDefine : LineDetailItem
    {
        private static readonly string RegexPatternEnd = @"^END(?:\s+(?<arg1>[^,]+)(?:\s*,\s*(?<arg2>[^,]+))?)?$";
        private static readonly Regex CompiledRegexPatternEnd = new Regex(
            RegexPatternEnd,
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase
        );
        private string EntryPointLabel { get; set; }
        private string LoadAddressLabel { get; set; }

        private UInt16? EntryPoint { get; set; }
        private UInt16? LoadAddress { get; set; }

        public override AsmList[] Lists
        {
            get
            {
                if (!AsmLoad.Share.IsOutputList)
                {
                    return new AsmList[] { };
                }

                return new[]
                {
                    AsmList.CreateLineItemEndDefine(EntryPoint, LineItem)
                };
            }
        }

        private LineDetailItemEndDefine(string entryPointLabel, string loadAddressLabel, LineItem lineItem, AsmLoad asmLoad)
            : base(lineItem, asmLoad)
        {
            EntryPointLabel = entryPointLabel;
            LoadAddressLabel = loadAddressLabel;
        }

        public static LineDetailItemEndDefine Create(LineItem lineItem, AsmLoad asmLoad)
        {
            if (!lineItem.IsCollectOperationString)
            {
                return default(LineDetailItemEndDefine);
            }

            var matched = CompiledRegexPatternEnd.Match(lineItem.OperationString);
            if (matched.Success)
            {
                var arg1 = matched.Groups["arg1"].Value;
                if (AIName.ValidateNameEndArgument(arg1)) // MACRO等のENDであるか確認する
                {
                    var arg2 = matched.Groups["arg2"].Value;
                    asmLoad.Scope.AssembleEndFlg = true;
                    return new LineDetailItemEndDefine(arg1, arg2, lineItem, asmLoad);
                }
            }

            return default(LineDetailItemEndDefine);
        }

        public override void Assemble()
        {
        }

        public override void PreAssemble(ref AsmAddress asmAddress)
        {
            base.PreAssemble(ref asmAddress);

            if (!string.IsNullOrEmpty(EntryPointLabel))
            {
                EntryPoint = AIMath.Calculation(EntryPointLabel, AsmLoad, asmAddress).ConvertTo<UInt16>();
                if (EntryPoint.HasValue)
                {
                    AsmLoad.Share.EntryPoint.SetByDefined(EntryPoint.Value);
                }
            }

            if (!string.IsNullOrEmpty(LoadAddressLabel))
            {
                LoadAddress = AIMath.Calculation(LoadAddressLabel, AsmLoad, asmAddress).ConvertTo<UInt16>();
                if (LoadAddress.HasValue)
                {
                    AsmLoad.Share.LoadAddress.SetByDefined(LoadAddress.Value);
                }
            }
        }

        public override void ExpansionItem()
        {
        }
    }
}
