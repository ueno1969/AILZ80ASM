﻿using AILZ80ASM.AILight;
using AILZ80ASM.Exceptions;
using AILZ80ASM.LineDetailItems.ScopeItem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AILZ80ASM.Assembler
{
    public class Macro
    {
        public string GlobalLabelName { get; private set; }
        public string Name { get; private set; }
        public string FullName => $"{GlobalLabelName}.{Name}";

        public string[] Args { get; private set; }
        public LineItem[] LineItems { get; private set; }

        private static readonly string RegexPatternMacro = @"^(?<macro>[a-zA-Z0-9_\.\(\)]+)\s*(?<args>.*)$";
        private static readonly Regex CompiledRegexPatternMacro = new Regex(
            RegexPatternMacro,
            RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase
        );

        public Macro(string macroName, string[] args, LineItem[] lineItems, AsmLoad asmLoad)
        {
            this.GlobalLabelName = asmLoad.Scope.GlobalLabelName;
            this.Name = macroName;

            Args = args;

            LineItems = lineItems;
        }

        public static Macro[] FindsWithoutNamespace(LineItem lineItem, AsmLoad asmLoad)
        {
            var operationMatched = CompiledRegexPatternMacro.Match(lineItem.OperationString);
            if (operationMatched.Success)
            {
                var macroName = operationMatched.Groups["macro"].Value;
                var macros = asmLoad.FindMacros(macroName);

                if (macros == default)
                {
                    return default;
                }
                return macros;
            }
            return default;

        }

        public static (Macro Macro, string[] Arguments) Find(LineItem lineItem, AsmLoad asmLoad)
        {
            var operationMatched = CompiledRegexPatternMacro.Match(lineItem.OperationString);
            if (operationMatched.Success)
            {
                var macroName = operationMatched.Groups["macro"].Value;
                var macroArgs = operationMatched.Groups["args"].Value;
                var macro = asmLoad.FindMacro(macroName);

                if (macro == default)
                {
                    return default;
                }
                var arguments = string.IsNullOrEmpty(macroArgs) ? Array.Empty<string>() : macroArgs.Split(',').Select(m => m.Trim()).ToArray();

                return (macro, arguments);
            }
            return default;
        }

        public MacroExpansionResult Expansion(LineItem lineItem, string[] arguments, AsmLoad asmLoad, ref AsmAddress asmAddress)
        {
            if (asmLoad.Share.LoadMacros.Any(m => this == m))
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E3008);
            }
            if (arguments.Length != this.Args.Length)
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E3004);
            }

            // Macro展開用のAsmLoadを作成する
            var guid = $"{Guid.NewGuid():N}";
            var lineItemList = new List<LineItem>();
            var localAsmAddress = asmAddress;
            var argumentLabelList = new List<Label>();

            asmLoad.CreateNewScope($"macro_{guid}", $"label_{guid}", localAsmLoad =>
            {
                if (arguments.Length > 0)
                {

                    // 引数の割り当て
                    foreach (var index in Enumerable.Range(0, arguments.Length))
                    {
                        var argumentLabel = new LabelMacroArg(arguments[index], asmLoad, lineItem, asmLoad, localAsmAddress);
                        var argumentValue = argumentLabel.DataType != Label.DataTypeEnum.Invalid ?
                                            argumentLabel.LabelFullName : arguments[index];

                        var label = new LabelMacroArg(this.Args[index], argumentValue, localAsmLoad, lineItem, asmLoad, localAsmAddress);
                        if (label.Invalidate)
                        {
                            throw new ErrorAssembleException(Error.ErrorCodeEnum.E3005);
                        }
                        localAsmLoad.AddLabel(label);
                        argumentLabelList.Add(label);
                    }
                }

                // LineItemsを作成
                var lineItems = this.LineItems.Skip(1).SkipLast(1).Select(m =>
                    {
                        var lineItem = new LineItem(m);
                        lineItem.CreateLineDetailItem(localAsmLoad);
                        return lineItem;
                    }).ToArray(); //　シーケンシャルに処理する必要があるため、ToArrayは必須

                foreach (var localLineItem in lineItems)
                {
                    try
                    {
                        localLineItem.ExpansionItem();
                    }
                    catch (ErrorAssembleException ex)
                    {
                        asmLoad.AddError(new ErrorLineItem(localLineItem, ex));
                    }
                    catch (ErrorLineItemException ex)
                    {
                        asmLoad.AddError(ex.ErrorLineItem);
                    }

                }
                lineItemList.AddRange(lineItems);

            });


            // 展開領域
            asmLoad.Share.LoadMacros.Push(this);

            foreach (var item in lineItemList)
            {
                try
                {
                    item.PreAssemble(ref asmAddress);
                }
                catch (ErrorAssembleException ex)
                {
                    asmLoad.AddError(new ErrorLineItem(item, ex));
                }
                catch (ErrorLineItemException ex)
                {
                    asmLoad.AddError(ex.ErrorLineItem);
                }
            }

            asmLoad.Share.LoadMacros.Pop();

            return new MacroExpansionResult() { LineItems = lineItemList.ToArray(), ArgumetLabels = argumentLabelList.ToArray() };
        }

        /// <summary>
        /// ロングマクロ名を生成する
        /// </summary>
        /// <param name="labelName"></param>
        /// <param name="asmLoad"></param>
        /// <returns></returns>
        public static string GetMacroFullName(string macroName, AsmLoad asmLoad)
        {
            if (macroName.IndexOf(".") > 0)
            {
                return macroName;
            }

            return $"{asmLoad.Scope.GlobalLabelName}.{macroName}";
        }
    }
}
