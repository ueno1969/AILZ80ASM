﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AILZ80ASM
{
    public abstract class LineDetailItemMacroDefine : LineDetailItem
    {
        private string MacroName { get; set; } = "";
        private string[] MacroArgs { get; set; }
        private readonly List<LineItem> MacroLines = new List<LineItem>();

        public override AsmList[] Lists
        {
            get
            {
                var lists = new List<AsmList>();
                foreach (var item in MacroLines)
                {
                    lists.Add(AsmList.CreateLineItem(item));
                }

                return lists.ToArray();
            }
        }

        protected LineDetailItemMacroDefine(LineItem lineItem, AsmLoad asmLoad)
            : base(lineItem, asmLoad)
        {

        }

        protected static LineDetailItemMacroDefine Create(LineDetailItemMacroDefine lineDetailItemMacroDefine, string regexPatternMacroStart, string regexPatternMacroEnd, AsmLoad asmLoad)
        {
            var lineItem = lineDetailItemMacroDefine.LineItem;
            var startMatched = Regex.Match(lineItem.OperationString, regexPatternMacroStart, RegexOptions.Singleline | RegexOptions.IgnoreCase);
            var endMatched = Regex.Match(lineItem.OperationString, regexPatternMacroEnd, RegexOptions.Singleline | RegexOptions.IgnoreCase);

            if (asmLoad.LineDetailItemForExpandItem != default &&
                asmLoad.LineDetailItemForExpandItem.GetType() == lineDetailItemMacroDefine.GetType())
            {
                var asmLoad_LineDetailItemMacro = asmLoad.LineDetailItemForExpandItem as LineDetailItemMacroDefine;
                if (startMatched.Success)
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E3001);
                }

                // ローカルラベル以外は使用禁止
                if (lineItem.LabelString.EndsWith(":"))
                {
                    asmLoad.LineDetailItemForExpandItem.Errors.Add(new ErrorLineItem(lineItem, Error.ErrorCodeEnum.E3006));
                }

                asmLoad_LineDetailItemMacro.MacroLines.Add(lineItem);

                if (endMatched.Success)
                {
                    // Macro登録
                    var lineDetailItemMacro = asmLoad_LineDetailItemMacro;
                    var macro = new Macro(lineDetailItemMacro.MacroName, lineDetailItemMacro.MacroArgs, lineDetailItemMacro.MacroLines.ToArray(), asmLoad);
                    //同名マクロチェック
                    if (asmLoad.Macros.Any(m => string.Compare(m.FullName, macro.FullName, true) == 0))
                    {
                        asmLoad.LineDetailItemForExpandItem.Errors.Add(new ErrorLineItem(asmLoad_LineDetailItemMacro.LineItem, Error.ErrorCodeEnum.E3010));
                    }

                    if (asmLoad.LineDetailItemForExpandItem.Errors.Count == 0)
                    {
                        asmLoad.Macros.Add(macro);
                    }
                    asmLoad.Errors.AddRange(asmLoad.LineDetailItemForExpandItem.Errors);

                    // 終了
                    asmLoad.LineDetailItemForExpandItem = default;
                }

                return lineDetailItemMacroDefine;
            }
            else
            {
                if (startMatched.Success)
                {
                    var args = new List<string>();
                    var argsText = startMatched.Groups["args"].Value;

                    if (!string.IsNullOrEmpty(argsText.Trim()))
                    {
                        args.AddRange(argsText.Split(',').Select(m => m.Trim()));
                    }

                    var lineDetailItemMacro = lineDetailItemMacroDefine;
                    lineDetailItemMacro.MacroName = startMatched.Groups["macro_name"].Value;
                    lineDetailItemMacro.MacroArgs = args.ToArray();
                    lineDetailItemMacro.MacroLines.Add(lineItem);

                    if (!AIName.ValidateMacroName(lineDetailItemMacro.MacroName, asmLoad))
                    {
                        lineDetailItemMacro.Errors.Add(new ErrorLineItem(lineItem, Error.ErrorCodeEnum.E3007));
                    }

                    var errorArgumentNames = args.Where(m => !AIName.ValidateMacroArgument(m, asmLoad));
                    if (errorArgumentNames.Any())
                    {
                        lineDetailItemMacro.Errors.Add(new ErrorLineItem(lineItem, Error.ErrorCodeEnum.E3005, string.Join(",", errorArgumentNames)));
                    }
                    asmLoad.LineDetailItemForExpandItem = lineDetailItemMacro;

                    return lineDetailItemMacro;
                }

                if (endMatched.Success)
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E3002);
                }
            }
            return default;
        }

        public override void ExpansionItem()
        {
            LineDetailScopeItems = Array.Empty<LineDetailScopeItem>();
        }

    }
}
