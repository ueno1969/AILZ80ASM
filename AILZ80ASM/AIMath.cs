﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace AILZ80ASM
{
    public static class AIMath
    {
        private enum MacroValueEnum
        {
            None,
            High,
            Low,
            Text,
        }

        private static readonly string RegexPatternErrorHexadecimal = @"(?<start>([\s|,]+)|(^))(?<value>(H[0-9A-Fa-f]+H))(?<end>(\s+)|($))";
        private static readonly string RegexPatternHexadecimal_H = @"(?<start>([\s|,]+)|(^))(?<value>([0-9A-Fa-f]+H))(?<end>(\s+)|($))";
        private static readonly string RegexPatternHexadecimal_X = @"(?<start>([\s|,]+)|(^))(?<value>(0x[0-9A-Fa-f]+))(?<end>(\s+)|($))";
        private static readonly string RegexPatternErrorDollarHexadecimal = @"(?<start>\s?)(?<value>(\$[0-9A-Fa-f]+\$))(?<end>\s?)";
        private static readonly string RegexPatternDollarHexadecimal = @"(?<start>\s?)(?<value>(\$[0-9A-Fa-f]+))(?<end>\s?)";
        private static readonly string RegexPatternErrorBinaryNumber = @"(?<start>\s?)(?<value>(%[01]+%))(?<end>\s?)";
        private static readonly string RegexPatternBinaryNumber = @"(?<start>\s?)(?<value>(^%[01_]+)|(^[01_]+B))(?<end>\s?)";
        private static readonly string RegexPatternLabel = @"(?<start>\s?)(?<value>([\w\.:@]+))(?<end>\s?)";
        private static readonly string RegexPatternDigit = @"^(\+|\-|)(\d+)$";
        private static readonly string RegexPatternFormuraAndDigit = @"^(\d+|\+|\-|\*|\/|\%|\~|\(|\)|!=|!|==|\<\<|\>\>|<=|\<|>=|\>|\&\&|\|\||\&|\||\^|\?|\:)";
        private static readonly string RegexPatternFormuraChar = @"^(\+|\-|\*|\/|\%|\~|\(|\)|!=|!|==|\<\<|\>\>|<=|\<|>=|\>|\&\&|\|\||\&|\||\^|\?|\:)$";
        private static readonly Dictionary<string, int> FormuraPriority = new()
        {
            [")"] = 1,
            ["!"] = 2,  ["~"] = 2, // 単項演算子は別で処理する ["+"] = 2,  ["-"] = 2,
            ["*"] = 3,  ["/"] = 3, ["%"] = 3,
            ["+"] = 4,  ["-"] = 4,
            ["<<"] = 5, [">>"] = 5,
            ["<"] = 6,  [">"] = 6, ["<="] = 6, [">="] = 6,
            ["=="] = 7, ["!="] = 7,
            ["&"] = 8,
            ["^"] = 9,
            ["|"] = 10,
            ["&&"] = 11,
            ["||"] = 12,
            ["?"] = 14, [":"] = 13,
            ["("] = 15,

        };

        public static bool IsNumber(string value)
        {
            if (Regex.Match(value, RegexPatternHexadecimal_H, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
            {
                return true;
            }
            if (Regex.Match(value, RegexPatternHexadecimal_X, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
            {
                return true;
            }

            if (Regex.Match(value, RegexPatternDollarHexadecimal, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
            {
                return true;
            }

            if (Regex.Match(value, RegexPatternBinaryNumber, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
            {
                return true;
            }

            if (decimal.TryParse(value, out var dammy))
            {
                return true;
            }

            return false;
        }

        public static bool TryParse<T>(string value, string globalLabelName, string labelName, AsmLoad asmLoad, AsmAddress asmAddress, out T resultValue)
            where T : struct
        {
            return InternalTryParse(value, globalLabelName, labelName, asmLoad, asmAddress, out resultValue);
        }

        public static bool TryParse<T>(string value, LineDetailExpansionItemOperation lineDetailExpansionItemOperation, AsmLoad asmLoad, AsmAddress asmAddress, out T resultValue)
            where T : struct
        {
            return TryParse<T>(value, lineDetailExpansionItemOperation.Label.GlobalLabelName, lineDetailExpansionItemOperation.Label.LabelName, asmLoad, asmAddress, out resultValue);
        }

        public static bool TryParse<T>(string value, string globalLabelName, string labelName, AsmLoad asmLoad, out T resultValue)
            where T : struct
        {
            return InternalTryParse(value, globalLabelName, labelName, asmLoad, default(AsmAddress?), out resultValue);
        }

        public static bool TryParse<T>(string value, LineDetailExpansionItemOperation lineDetailExpansionItemOperation, AsmLoad asmLoad, out T resultValue)
            where T : struct
        {
            return TryParse<T>(value, lineDetailExpansionItemOperation.Label.GlobalLabelName, lineDetailExpansionItemOperation.Label.LabelName, asmLoad, out resultValue);
        }

        public static bool TryParse<T>(string value, AsmLoad asmLoad, out T resultValue)
            where T : struct
        {
            return TryParse<T>(value, asmLoad.GlobalLabelName, asmLoad.LabelName, asmLoad, out resultValue);
        }

        public static T ConvertTo<T>(string value, AsmLoad asmLoad)
            where T : struct
        {
            return ConvertTo<T>(value, asmLoad.GlobalLabelName, asmLoad.LabelName, asmLoad);
        }

        public static T ConvertTo<T>(string value, LineDetailExpansionItemOperation lineDetailExpansionItemOperation, AsmLoad asmLoad)
            where T : struct
        {
            return ConvertTo<T>(value, lineDetailExpansionItemOperation.Label.GlobalLabelName, lineDetailExpansionItemOperation.Label.LabelName, asmLoad, lineDetailExpansionItemOperation.Address);
        }

        public static T ConvertTo<T>(string value, string globalLabelName, string labelName, AsmLoad asmLoad)
            where T : struct
        {
            return InternalConvertTo<T>(value, globalLabelName, labelName, asmLoad, default(AsmAddress?));
        }

        public static T ConvertTo<T>(string value, string globalLabelName, string labelName, AsmLoad asmLoad, AsmAddress asmAddress)
            where T : struct
        {
            return InternalConvertTo<T>(value, globalLabelName, labelName, asmLoad, asmAddress);
        }

        private static string ReplaceAll(string value, string globalLabelName, string labelName, AsmLoad asmLoad)
        {
            //16進数の置き換え
            value = Replace16Number(value);

            //2進数の置き換え
            value = ReplaceBinaryNumber(value);

            // ラベルの置き換え
            value = ReplaceLabel(value, globalLabelName, labelName, asmLoad);

            return value;
        }

        private static string ReplaceAll(string value, string globalLabelName, string labelName, AsmLoad asmLoad, AsmAddress address)
        {
            //16進数の置き換え
            value = Replace16NumberAndCurrentAddress(value, address);

            //2進数の置き換え
            value = ReplaceBinaryNumber(value);

            // ラベルの置き換え
            value = ReplaceLabel(value, globalLabelName, labelName, asmLoad);

            return value;
        }

        /// <summary>
        /// ラベル判別
        /// </summary>
        /// <param name="value"></param>
        /// <param name="globalLabelName"></param>
        /// <param name="labelName"></param>
        /// <param name="labels"></param>
        private static string ReplaceLabel(string value, string globalLabelName, string labelName, AsmLoad asmLoad)
        {
            var resultValue = "";
            var workValue = value;
            var limitCounter = 0;

            var regexResult = default(Match);
            while ((regexResult = Regex.Match(workValue, RegexPatternLabel, RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success && limitCounter < 10000)
            {
                var macroValue = MacroValueEnum.None;

                var matchResultString = regexResult.Groups["value"].Value;
                var index = workValue.IndexOf(matchResultString);
                var optionIndex = matchResultString.IndexOf(".@");
                var option = "";
                if (optionIndex > 0)
                {
                    option = matchResultString.Substring(optionIndex);
                    matchResultString = matchResultString.Substring(0, optionIndex);
                    if (string.Compare(option, ".@H", true) == 0 ||
                        string.Compare(option, ".@HIGH", true) == 0)
                    {
                        macroValue = MacroValueEnum.High;
                    }
                    else if (string.Compare(option, ".@L", true) == 0 ||
                             string.Compare(option, ".@LOW", true) == 0)
                    {
                        macroValue = MacroValueEnum.Low;
                    }
                    else if (string.Compare(option, ".@T", true) == 0 ||
                             string.Compare(option, ".@TEXT", true) == 0)
                    {
                        macroValue = MacroValueEnum.Text;
                    }
                }

                // ラベルチェック
                var labels = default(Label[]);
                if (macroValue == MacroValueEnum.Text)
                {
                    labels = asmLoad.AllLabels.ToArray();
                }
                else
                {
                    labels = asmLoad.AllLabels.Where(m => m.HasValue).ToArray();
                }

                var longLabelName = Label.GetLongLabelName(matchResultString, asmLoad);
                var label = labels.Where(m => string.Compare(m.LongLabelName, longLabelName, true) == 0).FirstOrDefault();
                matchResultString += option; // optionを元に戻す

                var valueString = "";
                switch (macroValue)
                {
                    case MacroValueEnum.High:
                        if (label == default)
                        {
                            valueString = matchResultString;
                        }
                        else
                        {
                            valueString = ((int)(label.Value / 256)).ToString("0");
                        }
                        break;
                    case MacroValueEnum.Low:
                        if (label == default)
                        {
                            valueString = matchResultString;
                        }
                        else
                        {
                            valueString = ((int)(label.Value % 256)).ToString("0");
                        }
                        break;
                    case MacroValueEnum.Text:
                        valueString = $"\"{label.ValueString}\"";
                        break;
                    default:
                        valueString = label?.Value.ToString("0") ?? matchResultString;
                        break;
                }

                resultValue += workValue.Substring(0, index);
                resultValue += valueString;
                workValue = workValue.Substring(index + matchResultString.Length);

                regexResult = Regex.Match(workValue, RegexPatternLabel, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                limitCounter++;
            }
            resultValue += workValue;

            return resultValue;
        }


        /// <summary>
        /// 16進数の変換
        /// </summary>
        /// <param name="value"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string Replace16Number(string value)
        {
            value = ReplaceHexadecimal(value);
            value = ReplaceDollarHexadecimal(value);

            return value;
        }

        /// <summary>
        /// 16進数の変換
        /// </summary>
        /// <param name="value"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public static string Replace16NumberAndCurrentAddress(string value, AsmAddress address)
        {
            value = Replace16Number(value);

            // $$の値を出力アドレスに置き換える
            value = value.Replace("$$", $"{address.Output:0}");

            // $の値をプログラムアドレスに置き換える
            value = value.Replace("$", $"{address.Program:0}");

            return value;
        }

        /// <summary>
        /// 16進数の変換(H)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="globalLabelName"></param>
        /// <param name="labelName"></param>
        /// <param name="labels"></param>
        private static string ReplaceHexadecimal(string value)
        {
            var resultValue = "";
            var workValue = value;
            var limitCounter = 0;

            if (Regex.Match(workValue, RegexPatternErrorHexadecimal, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E0005, $"対象：{value}");
            }

            var regexResult = default(Match);
            // 後ろH
            while ((regexResult = Regex.Match(workValue, RegexPatternHexadecimal_H, RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success && limitCounter < 10000)
            {
                var matchResultString = regexResult.Groups["value"].Value;
                var index = workValue.IndexOf(matchResultString, StringComparison.OrdinalIgnoreCase);

                resultValue += workValue.Substring(0, index);
                try
                {
                    resultValue += Convert.ToInt32(matchResultString.Substring(0, matchResultString.Length - 1), 16).ToString("0");
                }
                catch
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0005, $"対象：{value}");
                }
                workValue = workValue.Substring(index + matchResultString.Length);

                limitCounter++;
            }
            resultValue += workValue;

            workValue = resultValue;
            resultValue = "";
            // 前0x
            while ((regexResult = Regex.Match(workValue, RegexPatternHexadecimal_X, RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success && limitCounter < 10000)
            {
                var matchResultString = regexResult.Groups["value"].Value;
                var index = workValue.IndexOf(matchResultString, StringComparison.OrdinalIgnoreCase);

                resultValue += workValue.Substring(0, index);
                try
                {
                    resultValue += Convert.ToInt32(matchResultString.Substring(2, matchResultString.Length - 2), 16).ToString("0");
                }
                catch
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0005, $"対象：{value}");
                }
                workValue = workValue.Substring(index + matchResultString.Length);

                limitCounter++;
            }
            resultValue += workValue;

            return resultValue;
        }

        /// <summary>
        /// 16進数の変換($)
        /// </summary>
        /// <param name="value"></param>
        /// <param name="globalLabelName"></param>
        /// <param name="labelName"></param>
        /// <param name="labels"></param>
        private static string ReplaceDollarHexadecimal(string value)
        {
            var resultValue = "";
            var workValue = value;
            var limitCounter = 0;

            if (Regex.Match(workValue, RegexPatternErrorDollarHexadecimal, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E0005, $"対象：{value}");
            }

            var regexResult = default(Match);
            while ((regexResult = Regex.Match(workValue, RegexPatternDollarHexadecimal, RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success && limitCounter < 10000)
            {
                var matchResultString = regexResult.Groups["value"].Value;
                var index = workValue.IndexOf(matchResultString, StringComparison.OrdinalIgnoreCase);

                resultValue += workValue.Substring(0, index);
                try
                {
                    resultValue += Convert.ToInt32(matchResultString.Substring(1), 16).ToString("0");
                }
                catch
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0005, $"対象：{value}");
                }
                workValue = workValue.Substring(index + matchResultString.Length);

                limitCounter++;
            }
            resultValue += workValue;

            return resultValue;
        }


        /// <summary>
        /// 2進数の変換
        /// </summary>
        /// <param name="value"></param>
        /// <param name="globalLabelName"></param>
        /// <param name="labelName"></param>
        /// <param name="labels"></param>
        public static string ReplaceBinaryNumber(string value)
        {
            var resultValue = "";
            var workValue = value;
            var limitCounter = 0;

            if (Regex.Match(workValue, RegexPatternErrorBinaryNumber, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E0008, $"対象：{value}");
            }

            var regexResult = default(Match);
            while ((regexResult = Regex.Match(workValue, RegexPatternBinaryNumber, RegexOptions.Singleline | RegexOptions.IgnoreCase)).Success && limitCounter < 10000)
            {
                var matchResultString = regexResult.Groups["value"].Value;
                var index = workValue.IndexOf(matchResultString, StringComparison.OrdinalIgnoreCase);

                resultValue += workValue.Substring(0, index);
                try
                {
                    var target = matchResultString;
                    foreach (var item in "_%Bb".ToArray())
                    {
                        target = target.Replace(item.ToString(), "");
                    }

                    resultValue += Convert.ToInt32(target, 2).ToString("0");
                }
                catch
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0008, $"対象：{value}");
                }
                workValue = workValue.Substring(index + matchResultString.Length);

                limitCounter++;
            }
            resultValue += workValue;

            return resultValue;
        }

        /// <summary>
        /// 式の文字列から演算を行う
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        public static T Calculation<T>(string target)
            where T : struct
        {
            var terms = CalculationParse(target);
            var rvpns = CalculationMakeReversePolish(terms);
            var value = CalculationByReversePolish<T>(rvpns);

            return value;
        }

        /// <summary>
        /// 式を分解する
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static string[] CalculationParse(string value)
        {
            var terms = new List<string>();
            var tmpValue = value.Trim();

            // 数値と演算子に分解する
            while (!string.IsNullOrEmpty(tmpValue))
            {
                var matched = Regex.Match(tmpValue, RegexPatternFormuraAndDigit, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (matched.Success)
                {
                    terms.Add(matched.Value);
                    tmpValue = tmpValue.Substring(matched.Length).TrimStart();
                }
                else if (tmpValue.StartsWith("\""))
                {
                    var endIndex = tmpValue.IndexOf("\"", 1);
                    var escapeIndex = tmpValue.IndexOf("\\", 1);
                    while (endIndex != -1 && escapeIndex != -1 && endIndex - 1 == escapeIndex)
                    {
                        endIndex = tmpValue.IndexOf("\"", endIndex + 1);
                        escapeIndex = tmpValue.IndexOf("\\", endIndex + 1);
                    }
                    if (endIndex == -1)
                    {
                        throw new Exception("演算に使えない文字が検出されました。");
                    }
                    else
                    {
                        terms.Add(tmpValue.Substring(0, endIndex + 1));
                        tmpValue = tmpValue.Substring(endIndex + 1).TrimStart();
                    }
                }
                else
                {
                    throw new Exception("演算に使えない文字が検出されました。");
                }
            }

            var result = new List<string>();
            var sign = "";
            // 単項演算子を結合する
            foreach (var index in Enumerable.Range(0, terms.Count))
            {
                var tmpString = terms[index];
                if (tmpString == "+" || tmpString == "-")
                {
                    if (index == 0 || terms[index - 1] != ")" && Regex.Match(terms[index - 1], RegexPatternFormuraChar, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
                    {
                        sign = tmpString;
                        continue;
                    }
                }
                result.Add(sign + tmpString);
                sign = "";
            }
            if (!string.IsNullOrEmpty(sign))
            {
                result.Add(sign);
            }

            // 演算子、数値が連続しているものがないか確認をする
            var checkValues = result.Where(m => m != "(" && m != ")").ToArray();
            foreach (var index in Enumerable.Range(0, checkValues.Length - 1))
            {
                if (Regex.Match(checkValues[index + 0], RegexPatternDigit,RegexOptions.Singleline | RegexOptions.IgnoreCase).Success &&
                    Regex.Match(checkValues[index + 1], RegexPatternDigit, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
                {
                    throw new Exception("数値と数値の間には演算子が必要です");
                }

                if (Regex.Match(checkValues[index + 0], RegexPatternFormuraChar, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success &&
                    Regex.Match(checkValues[index + 1], RegexPatternFormuraChar, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
                {
                    throw new Exception("演算子が連続で指定されています。");
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// 逆ポーランド記法に変換する
        /// </summary>
        /// <param name="terms"></param>
        /// <returns></returns>
        private static string[] CalculationMakeReversePolish(string[] terms)
        {
            var result = new List<string>();
            var formura = new Stack<string>();

            foreach (var item in terms)
            {
                var matched = Regex.Match(item, RegexPatternDigit, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                if (matched.Success || (item.StartsWith("\"") && item.EndsWith("\"")))
                {
                    result.Add(item);
                }
                else
                {
                    while (true)
                    {
                        if (item == ")")
                        {
                            while (formura.Count > 0 && formura.Peek() != "(")
                            {
                                result.Add(formura.Pop());
                            }
                            if (formura.Count == 0)
                            {
                                throw new Exception("括弧の数が不一致です");
                            }
                            formura.Pop();
                            break;
                        }
                        else if (formura.Count == 0 || item == "(" || FormuraPriority[formura.Peek()] > FormuraPriority[item])
                        {
                            formura.Push(item);
                            break;
                        }
                        else
                        {
                            result.Add(formura.Pop());
                        }
                    }
                }
            }

            result.AddRange(formura);
            if (result.Any(m => m == "("))
            {
                throw new Exception("括弧の数が不一致です");
            }

            // 三項演算子のチェック
            var checkValues = result.ToArray();
            foreach (var index in Enumerable.Range(0, checkValues.Length - 1))
            {
                if ((checkValues[index + 0] == ":" && checkValues[index + 1] != "?") ||
                    (checkValues[index + 0] != ":" && checkValues[index + 1] == "?"))
                {
                    throw new Exception("三項演算子の使い方が間違っています。");
                }
            }

            return result.ToArray();
        }

        /// <summary>
        /// 逆ポーランド記法から演算を行う
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="rpns"></param>
        /// <returns></returns>
        private static T CalculationByReversePolish<T>(string[] rpns)
            where T : struct
        {
            var stack = new Stack<object>();

            foreach (var item in rpns)
            {
                if (Regex.Match(item, RegexPatternDigit, RegexOptions.Singleline | RegexOptions.IgnoreCase).Success)
                {
                    if (int.TryParse(item, out var result))
                    {
                        stack.Push(result);
                    }
                    else
                    {
                        throw new Exception("数値に変換できませんでした。");
                    }
                }
                else if (item.StartsWith("\"") && item.EndsWith("\""))
                {
                    stack.Push(item.Substring(1, item.Length - 2));
                }
                else
                {
                    switch (item)
                    {
                        case "+":
                        case "-":
                        case "*":
                        case "/":
                        case "%":
                        case "<<":
                        case ">>":
                        case ">":
                        case ">=":
                        case "<":
                        case "<=":
                        case "==":
                        case "!=":
                        case "&":
                        case "^":
                        case "|":
                            {
                                if (stack.Count < 2)
                                {
                                    throw new Exception("演算に失敗しました。");
                                }

                                var lastPopValue = stack.Pop();
                                var firstPopValue = stack.Pop();

                                if (lastPopValue is int lastValue &&
                                    firstPopValue is int firstValue)
                                {
                                    switch (item)
                                    {
                                        case "+":
                                            stack.Push(firstValue + lastValue);
                                            break;
                                        case "-":
                                            stack.Push(firstValue - lastValue);
                                            break;
                                        case "*":
                                            stack.Push(firstValue * lastValue);
                                            break;
                                        case "/":
                                            stack.Push(firstValue / lastValue);
                                            break;
                                        case "%":
                                            stack.Push(firstValue % lastValue);
                                            break;
                                        case "<<":
                                            stack.Push(firstValue << lastValue);
                                            break;
                                        case ">>":
                                            stack.Push(firstValue >> lastValue);
                                            break;
                                        case ">":
                                            stack.Push(firstValue > lastValue);
                                            break;
                                        case ">=":
                                            stack.Push(firstValue >= lastValue);
                                            break;
                                        case "<":
                                            stack.Push(firstValue < lastValue);
                                            break;
                                        case "<=":
                                            stack.Push(firstValue <= lastValue);
                                            break;
                                        case "==":
                                            stack.Push(firstValue == lastValue);
                                            break;
                                        case "!=":
                                            stack.Push(firstValue != lastValue);
                                            break;
                                        case "&":
                                            stack.Push(firstValue & lastValue);
                                            break;
                                        case "^":
                                            stack.Push(firstValue ^ lastValue);
                                            break;
                                        case "|":
                                            stack.Push(firstValue | lastValue);
                                            break;
                                        default:
                                            throw new NotImplementedException();
                                    }
                                }else if (lastPopValue is string lastStringValue &&
                                          firstPopValue is string firstStringValue)
                                {
                                    switch (item)
                                    {
                                        case "+":
                                            stack.Push(firstStringValue + lastStringValue);
                                            break;
                                        case "==":
                                            stack.Push(string.Compare(firstStringValue, lastStringValue, true) == 0);
                                            break;
                                        case "!=":
                                            stack.Push(string.Compare(firstStringValue, lastStringValue, true) != 0);
                                            break;
                                        default:
                                            throw new NotImplementedException();
                                    }
                                }
                                else
                                {
                                    throw new Exception($"演算に使う値の型が一致していません。{lastPopValue}{item}{firstPopValue}");
                                }


                            }
                            break;
                        case "&&":
                        case "||":
                            {
                                if (stack.Count < 2)
                                {
                                    throw new Exception("演算に失敗しました。");
                                }

                                var lastValue = (bool)stack.Pop();
                                var firstValue = (bool)stack.Pop();

                                switch (item)
                                {
                                    case "&&":
                                        stack.Push(firstValue && lastValue);
                                        break;
                                    case "||":
                                        stack.Push(firstValue || lastValue);
                                        break;
                                    default:
                                        throw new NotImplementedException();
                                }
                            }
                            break;
                        case "!":
                            {
                                if (stack.Count < 1)
                                {
                                    throw new Exception("演算に失敗しました。");
                                }

                                var firstValue = stack.Pop();
                                if (firstValue is bool boolValue)
                                {
                                    stack.Push(!boolValue);
                                }
                                else if (firstValue is int intValue)
                                {
                                    stack.Push(~intValue);
                                }
                            }
                            break;
                        case "~":
                            {
                                if (stack.Count < 1)
                                {
                                    throw new Exception("演算に失敗しました。");
                                }

                                var firstValue = (int)stack.Pop();
                                stack.Push(~firstValue);
                            }
                            break;
                        case ":": 
                            // 三項演算子の記号、?で処理するのでここは無処理
                            break;
                        case "?":
                            {
                                if (stack.Count < 3)
                                {
                                    throw new Exception("演算に失敗しました。");
                                }

                                var falseValue = stack.Pop();
                                var trueValue = stack.Pop();
                                var conditionValue = (bool)stack.Pop();
                                if (conditionValue)
                                {
                                    stack.Push(trueValue);
                                }
                                else
                                {
                                    stack.Push(falseValue);
                                }
                            }
                            break;

                    }
                }
            }

            return CalculationNormalization<T>(stack.Pop());
        }

        /// <summary>
        /// 指定の型に変換する
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <returns></returns>
        private static T CalculationNormalization<T>(object value)
            where T : struct
        {
            if (value is int intValue)
            {
                if (typeof(T) == typeof(int))
                {
                    return (T)(object)intValue;
                }
                else if (typeof(T) == typeof(UInt32))
                {
                    if (intValue < 0)
                    {
                        return (T)(object)Convert.ToUInt32(UInt32.MaxValue + intValue + 1);
                    }
                    else
                    {
                        return (T)(object)Convert.ToUInt32(intValue & UInt32.MaxValue);
                    }
                }
                else if (typeof(T) == typeof(UInt16))
                {
                    if (intValue < 0)
                    {
                        return (T)(object)Convert.ToUInt16(UInt16.MaxValue + intValue + 1);
                    }
                    else
                    {
                        return (T)(object)Convert.ToUInt16(intValue & UInt16.MaxValue);
                    }
                }
                else if (typeof(T) == typeof(byte))
                {
                    if (intValue < 0)
                    {
                        return (T)(object)Convert.ToByte(byte.MaxValue + intValue + 1);
                    }
                    else
                    {
                        return (T)(object)Convert.ToByte(intValue & byte.MaxValue);
                    }
                }
                else
                {
                    throw new ArgumentException(nameof(intValue));
                }
            }
            else if (typeof(T) == typeof(bool) && value is bool boolValue)
            {
                return (T)(object)boolValue;
            }
            else
            {
                throw new ArgumentException(nameof(value));
            }
        }

        
        /// <summary>
        /// 演算可能かを判断し、可能なら演算をする
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="globalLabelName"></param>
        /// <param name="labelName"></param>
        /// <param name="asmLoad"></param>
        /// <param name="resultValue"></param>
        /// <returns></returns>
        private static bool InternalTryParse<T>(string value, string globalLabelName, string labelName, AsmLoad asmLoad, AsmAddress? asmAddress, out T resultValue)
            where T : struct
        {
            var tmpValue = default(string);

            if (asmAddress.HasValue)
            {
                tmpValue = ReplaceAll(value, globalLabelName, labelName, asmLoad, asmAddress.Value);
            }
            else
            {
                tmpValue = ReplaceAll(value, globalLabelName, labelName, asmLoad);
            }

            try
            {
                resultValue = Calculation<T>(tmpValue);
                return true;
            }
            catch
            {
                resultValue = default(T);
                return false;
            }
        }

        /// <summary>
        /// コンバートを行う
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="globalLabelName"></param>
        /// <param name="labelName"></param>
        /// <param name="asmLoad"></param>
        /// <param name="asmAddress"></param>
        /// <returns></returns>
        private static T InternalConvertTo<T>(string value, string globalLabelName, string labelName, AsmLoad asmLoad, AsmAddress? asmAddress)
            where T : struct
        {
            if (InternalTryParse<T>(value, globalLabelName, labelName, asmLoad, asmAddress, out var resultValue))
            {
                return resultValue;
            }
            else
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E0004, $"演算対象：{value}");
            }
        }
    }
}
