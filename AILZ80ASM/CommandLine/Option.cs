﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using AILZ80ASM.Assembler;

namespace AILZ80ASM.CommandLine
{
    public class Option<T> : IOption
    {
        public string Name { get; set; }
        public string ArgumentName { get; set; }
        public string[] Aliases { get; set; }
        public string Description { get; set; }
        public bool Required { get; set; }
        public bool IsDefineOptional { get; set; }                      // オプションコマンドの指定を省略可能
        public Func<string[], string> OptionFunc { get; set; }
        public bool HasValue { get; set; }
        public bool Selected { get; set; }
        public bool IsHide { get;set; }                                 // 非表示
        public bool IsSimple { get; set; }                              // シンプルヘルプ
        public bool IsShortCut { get; set; }                            // ショートカット
        public bool IsHelp { get; set; }                                // ヘルプコマンド
        public string DefaultValue { get; set; }
        public Parameter[] Parameters { get; set; }
        public Func<IOption[], string[]> DefaultFunc { get; set; }
        public T Value { get; set; } = default(T);

        public void Clear()
        {
            Selected = false;
            HasValue = false;
            Value = default(T);
        }

        public void SetValue(string[] values)
        {
            Selected = true;

            var localValues = values.ToList();
            if (localValues.Count == 0 && DefaultFunc != default)
            {
                return;
            }

            if (typeof(T) == typeof(FileInfo))
            {
                if (localValues.Count != 1)
                {
                    throw new Exception($"{Name}に、ファイルを指定する必要があります。（1ファイルのみ指定可能）");
                }

                Value = (T)(object)new FileInfo(localValues.First());
                HasValue = true;
            }
            else if (typeof(T) == typeof(FileInfo[]))
            {
                if (localValues.Count == 0)
                {
                    throw new Exception($"{Name}に、ファイルを指定する必要があります。（複数ファイル指定可能）");
                }

                Value = (T)(object)localValues.Select(m => new FileInfo(m)).ToArray();
                HasValue = true;
            }
            else if (typeof(T) == typeof(DirectoryInfo))
            {
                if (localValues.Count != 1)
                {
                    throw new Exception($"{Name}に、ディレクトリを指定する必要があります。（1ディレクトリのみ指定可能）");
                }

                Value = (T)(object)new DirectoryInfo(localValues.First());
                HasValue = true;
            }
            else if (typeof(T) == typeof(int))
            {
                if (localValues.Count != 1)
                {
                    throw new Exception($"{Name}に、数値を指定する必要があります。");
                }

                Value = (T)(object)Convert.ToInt32(localValues[0]);
                HasValue = true;
            }
            else if (typeof(T) == typeof(byte))
            {
                if (localValues.Count != 1)
                {
                    throw new Exception($"{Name}に、数値を指定する必要があります。");
                }
                Value = (T)(object)AILight.AIMath.Calculation(localValues[0]).ConvertTo<byte>();
                HasValue = true;
            }
            else if (typeof(T) == typeof(bool))
            {
                Value = (T)(object)true;
                HasValue = true;
            }
            else if (typeof(T) == typeof(string))
            {
                if (localValues.Count > 1 && string.IsNullOrEmpty(DefaultValue))
                {
                    var optionName = "値";
                    if (Parameters != default)
                    {
                        optionName = string.Join(", ", Parameters.Select(m => m.Name));
                    }
                    throw new Exception($"{Name}に、{optionName}を指定する必要があります。");
                }

                if (localValues.Count == 0)
                {
                    Value = (T)(object)DefaultValue;
                }
                else
                {
                    var parameterName = localValues[0];
                    if (Parameters != default)
                    {
                        var parameter = Parameters.FirstOrDefault(m => string.Compare(m.Name, parameterName, true) == 0);
                        if (parameter != default)
                        {
                            Value = (T)(object)parameter.Name;
                        }
                        else
                        {
                            throw new Exception($"{Name}に、{string.Join(", ", Parameters.Select(m => m.Name))}を指定する必要があります。");
                        }
                    }
                    else
                    {
                        Value = (T)(object)parameterName;
                    }
                }
                HasValue = true;
            }
            else if (typeof(T) == typeof(Error.ErrorCodeEnum[]))
            {
                if (localValues.Count == 0)
                {
                    throw new Exception($"{Name}に、ワーニング・コードを指定する必要があります。（複数コード指定可能）");
                }
                // 全部変換できるか確認
                var result = new List<Error.ErrorCodeEnum>();
                foreach (var item in localValues)
                {
                    if (Enum.TryParse<Error.ErrorCodeEnum>(item, out var errorCode))
                    {
                        result.Add(errorCode);
                    }
                    else
                    {
                        throw new Exception($"{Name}に、{item}は指定できません。ワーニング・コードを確認してください。");
                    }
                }

                Value = (T)(object)result.ToArray();
                HasValue = true;
            }
            else
            {
                throw new NotImplementedException();
            }    
        }
    }
}