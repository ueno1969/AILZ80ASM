﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AILZ80ASM.InstructionSet;
using AILZ80ASM.Exceptions;
using AILZ80ASM.AILight;
using AILZ80ASM.LineDetailItems;
using AILZ80ASM.LineDetailItems.ScopeItem;

namespace AILZ80ASM.Assembler
{
    public class AsmLoad
    {
        // アセンブルオプション
        public AsmOption AssembleOption { get; private set; }

        // 命令セット
        public ISA ISA { get; private set; } 

        // 共有データ
        public AsmLoadShare Share { get; private set; }

        // スコープデータ
        public AsmLoadScope Scope { get; private set; }

        // スコープの親
        private AsmLoad ParentAsmLoad { get; set; } = default;

        /// <summary>
        /// アセンブルエラー
        /// </summary>
        public ErrorLineItem[] AssembleErrors
        {
            get
            {
                return Share.Errors.Where(m => m.ErrorType == Error.ErrorTypeEnum.Error && (AssembleOption.DisableWarningCodes == default || !AssembleOption.DisableWarningCodes.Any(n => m.ErrorCode == n))).ToArray();
            }
        }

        /// <summary>
        /// アセンブルワーニング
        /// </summary>
        public ErrorLineItem[] AssembleWarnings
        {
            get
            {
                return Share.Errors.Where(m => m.ErrorType == Error.ErrorTypeEnum.Warning && (AssembleOption.DisableWarningCodes == default || !AssembleOption.DisableWarningCodes.Any(n => m.ErrorCode == n))).ToArray();
            }
        }

        /// <summary>
        /// アセンブルインフォメーション
        /// </summary>
        public ErrorLineItem[] AssembleInformation
        {
            get
            {
                return Share.Errors.Where(m => m.ErrorType == Error.ErrorTypeEnum.Information && (AssembleOption.DisableWarningCodes == default || !AssembleOption.DisableWarningCodes.Any(n => m.ErrorCode == n))).ToArray();
            }
        }

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="assembleOption"></param>
        /// <param name="isa"></param>
        public AsmLoad(AsmOption assembleOption, ISA isa)
        {
            AssembleOption = assembleOption;
            ISA = isa;

            Share = new AsmLoadShare();
            Share.Errors = new List<ErrorLineItem>();
            Share.AsmORGs = new List<AsmORG>() { new AsmORG() };
            Share.LoadFiles = new Stack<FileInfo>();
            Share.LoadMacros = new Stack<Macro>();
            Share.ListedFiles = new List<FileInfo>();
            Share.PragmaOnceFiles = new List<FileInfo>();

            Scope = new AsmLoadScope();
            Scope.Labels = new List<Label>();
            Scope.Labels = new List<Label>();
            Scope.Macros = new List<Macro>();
            Scope.Functions = new List<Function>();
            Scope.GlobalLabelNames = new List<string>();
        }

        /// <summary>
        /// 新しいスコープで処理をする
        /// </summary>
        /// <param name="func"></param>
        public void CreateScope(Action<AsmLoad> func)
        {
            var asmLoad = new AsmLoad(this.AssembleOption, this.ISA)
            {
                Share = this.Share,
            };
            asmLoad.Scope = this.Scope.CreateScope();
            asmLoad.ParentAsmLoad = this.ParentAsmLoad;

            func.Invoke(asmLoad);

            this.Scope.Restore(asmLoad.Scope);
        }

        /// <summary>
        /// 新しいローカルスコープを作成
        /// </summary>
        /// <param name="globalLabelName"></param>
        /// <param name="labelName"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public int CreateLocalScope(string globalLabelName, string labelName, Func<AsmLoad, int> func)
        {
            var asmLoad = new AsmLoad(this.AssembleOption, this.ISA)
            {
                Share = this.Share,
            }; 
            asmLoad.Scope = this.Scope.CreateLocalScope();
            asmLoad.Scope.GlobalLabelName = globalLabelName;
            asmLoad.Scope.LabelName = labelName;
            asmLoad.Scope.GlobalLabelNames.Add(globalLabelName);
            asmLoad.ParentAsmLoad = this;


            return func.Invoke(asmLoad);
        }

        /// <summary>
        /// 新しいローカルスコープを作成
        /// </summary>
        /// <param name="globalLabelName"></param>
        /// <param name="labelName"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        public AIValue CreateLocalScope2(string globalLabelName, string labelName, Func<AsmLoad, AIValue> func)
        {
            var asmLoad = new AsmLoad(this.AssembleOption, this.ISA)
            {
                Share = this.Share,
            };
            asmLoad.Scope = this.Scope.CreateLocalScope();
            asmLoad.Scope.GlobalLabelName = globalLabelName;
            asmLoad.Scope.LabelName = labelName;
            asmLoad.Scope.GlobalLabelNames.Add(globalLabelName);
            asmLoad.ParentAsmLoad = this;


            return func.Invoke(asmLoad);
        }

        /// <summary>
        /// 新しいスコープを作成する
        /// </summary>
        /// <param name="globalLabelName"></param>
        /// <param name="labelName"></param>
        /// <param name="action"></param>
        public void CreateNewScope(string globalLabelName, string labelName, Action<AsmLoad> action)
        {
            CreateLocalScope(globalLabelName, labelName, localAsmLoad => 
            {
                action.Invoke(localAsmLoad);

                return 0;
            });
        }

        /// <summary>
        /// ラベルの処理を行います
        /// </summary>
        /// <param name="func"></param>
        /// <param name="lineItem"></param>
        /// <returns></returns>
        public LineDetailItem ProcessLabel(Func<LineDetailItem> func, LineItem lineItem)
        {
            // ラベル前処理
            ProcessLabel_Pre(lineItem);

            var lineDetailItem = func.Invoke();

            // ラベル後処理
            ProcessLabel_Post(lineDetailItem);

            return lineDetailItem;
        }

        /// <summary>
        /// ラベルの前処理
        /// </summary>
        /// <param name="lineItem"></param>
        /// <exception cref="ErrorAssembleException"></exception>
        private void ProcessLabel_Pre(LineItem lineItem)
        {
            if (string.IsNullOrEmpty(lineItem.LabelString))
            {
                return;
            }

            var label = new LabelAdr(lineItem.LabelString, this);
            if (label.LabelLevel == Label.LabelLevelEnum.GlobalLabel)
            {
                // ラベルと同じ名前は付けられない
                if (this.Scope.Labels.Any(m => string.Compare(m.LabelName, label.LabelFullName, true) == 0))
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0017);
                }

                if (!this.Scope.GlobalLabelNames.Any(m => string.Compare(m, label.LabelFullName, true) == 0))
                {
                    // ネームスペースが変わるときには保存する
                    this.Scope.GlobalLabelNames.Add(label.LabelFullName);
                }
                this.Scope.GlobalLabelName = label.GlobalLabelName;
                this.Scope.LabelName = label.LabelName;
            }
            else
            {
                // ネームスペースとと同じ名前は付けられない
                if (this.Scope.GlobalLabelNames.Any(m => string.Compare(m, label.LabelName, true) == 0))
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0018);
                }
                this.Scope.LabelName = label.LabelName;
            }
        }

        /// <summary>
        /// ラベルの後処理
        /// </summary>
        /// <param name="lineDetailItem"></param>
        /// <exception cref="ErrorAssembleException"></exception>
        private void ProcessLabel_Post(LineDetailItem lineDetailItem)
        {
            // ラベルを処理する
            if (!string.IsNullOrEmpty(lineDetailItem.LineItem.LabelString))
            {
                var label = default(Label);
                if (lineDetailItem is LineDetailItemEqual lineDetailItemEqual)
                {
                    label = new LabelEqu(lineDetailItemEqual, this);
                    lineDetailItemEqual.EquLabel = label;
                }
                else
                {
                    label = new LabelAdr(lineDetailItem, this);
                }

                if (label.Invalidate)
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0013);
                }

                this.AddLabel(label);
                // ローカルラベルの末尾に「:」がついている場合にはワーニング
                if (lineDetailItem.LineItem.LabelString.StartsWith(".") && lineDetailItem.LineItem.LabelString.EndsWith(":"))
                {
                    this.AddError(new ErrorLineItem(lineDetailItem.LineItem, Error.ErrorCodeEnum.W9005));
                }
            }
        }

        /// <summary>
        /// ラベルをビルドする（値を確定させる）
        /// </summary>
        public void BuildLabel()
        {
            foreach (var item in this.Scope.Labels.Where(m => m.DataType == Label.DataTypeEnum.None))
            {
                item.BuildLabel();
            }
        }

        /// <summary>
        /// マクロ関連が閉じられているかを確認する
        /// </summary>
        public void LoadCloseValidate()
        {
            if (this.Share.LineDetailItemForExpandItem is LineDetailItemMacroDefine)
            {
                this.AddError(new ErrorLineItem(this.Share.LineDetailItemForExpandItem.LineItem, Error.ErrorCodeEnum.E3001));
            }

            if (this.Share.LineDetailItemForExpandItem is LineDetailItemRepeat)
            {
                this.AddError(new ErrorLineItem(this.Share.LineDetailItemForExpandItem.LineItem, Error.ErrorCodeEnum.E1011));
            }

            if (this.Share.LineDetailItemForExpandItem is LineDetailItemConditional)
            {
                this.AddError(new ErrorLineItem(this.Share.LineDetailItemForExpandItem.LineItem, Error.ErrorCodeEnum.E1021));
            }
        }

        /// <summary>
        /// エラーの追加
        /// </summary>
        /// <param name="errorLineItem"></param>
        public void AddError(ErrorLineItem errorLineItem)
        {
            Share.Errors.Add(errorLineItem);
        }

        /// <summary>
        /// 複数エラーの追加
        /// </summary>
        /// <param name="errorLineItems"></param>
        public void AddErrors(IEnumerable<ErrorLineItem> errorLineItems)
        {
            Share.Errors.AddRange(errorLineItems);
        }

        /// <summary>
        /// ラベルの追加
        /// </summary>
        /// <param name="label"></param>
        /// <exception cref="ErrorAssembleException"></exception>
        public void AddLabel(Label label)
        {
            // 同一名のラベル
            if (this.Scope.Labels.Any(m => string.Compare(m.LabelFullName, label.LabelFullName, true) == 0))
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E0014);
            }
            if (label.LabelLevel == Label.LabelLevelEnum.GlobalLabel)
            {
                // ラベルと同じ名前は付けられない
                if (this.Scope.Labels.Any(m => string.Compare(m.LabelName, label.LabelFullName, true) == 0))
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0017);
                }

                if (!this.Scope.GlobalLabelNames.Any(m => string.Compare(m, label.LabelFullName, true) == 0))
                {
                    // ネームスペースが変わるときには保存する
                    this.Scope.GlobalLabelNames.Add(label.LabelFullName);
                }
                this.Scope.GlobalLabelName = label.GlobalLabelName;
                this.Scope.LabelName = label.LabelName;
            }
            else
            {
                // ネームスペースとと同じ名前は付けられない
                if (this.Scope.GlobalLabelNames.Any(m => string.Compare(m, label.LabelName, true) == 0))
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0018);
                }
                this.Scope.Labels.Add(label);
                this.Scope.LabelName = label.LabelName;
            }

        }

        /// <summary>
        /// ファンクションの追加
        /// </summary>
        /// <param name="function"></param>
        /// <exception cref="ErrorAssembleException"></exception>
        public void AddFunction(Function function)
        {
            if (this.Scope.Functions.Any(m => string.Compare(m.FullName, function.FullName, true) == 0))
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E4001);
            }
            this.Scope.Functions.Add(function);
        }

        /// <summary>
        /// マクロの追加
        /// </summary>
        /// <param name="macro"></param>
        /// <exception cref="ErrorAssembleException"></exception>
        public void AddMacro(Macro macro)
        {
            if (this.Scope.Macros.Any(m => string.Compare(m.FullName, macro.FullName, true) == 0))
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E3010);
            }
            this.Scope.Macros.Add(macro);
        }

        /// <summary>
        /// ORGにぶら下がる明細を追加
        /// </summary>
        /// <param name="lineDetailItem"></param>
        public void AddLineDetailItem(LineDetailItem lineDetailItem)
        {
            this.Share.AsmORGs.Last().AddLineDetailItem(lineDetailItem);
        }

        /// <summary>
        /// 読み込みファイルを追加
        /// </summary>
        /// <param name="fileInfo"></param>
        public void AddListedFile(FileInfo fileInfo)
        {
            this.Share.ListedFiles.Add(fileInfo);
        }

        /// <summary>
        /// AsmORGを追加
        /// </summary>
        /// <param name="asmORG"></param>
        public void AddORG(AsmORG asmORG)
        {
            this.Share.AsmORGs.Add(asmORG);
        }

        /// <summary>
        /// Pragma
        /// </summary>
        /// <param name="fileInfo"></param>
        public void AddPragmaOnceFileInfo(FileInfo fileInfo)
        {
            this.Share.PragmaOnceFiles.Add(fileInfo);
        }

        public bool ListedFileExists(FileInfo fileInfo)
        {
            return this.Share.ListedFiles.Any(m => m.FullName == fileInfo.FullName);
        }

        public string FindGlobalLabelName(string target)
        {
            var targetAsmLoad = this;

            while (targetAsmLoad != default)
            {
                var name = targetAsmLoad.Scope.GlobalLabelNames.Where(m => string.Compare(m, target, true) == 0).FirstOrDefault();
                if (name != default)
                {
                    return name;
                }
                targetAsmLoad = targetAsmLoad.ParentAsmLoad;
            }
            return default;
        }

        public Label FindLabel(string target)
        {
            var targetAsmLoad = this;

            while (targetAsmLoad != default)
            {
                var labelFullName = Label.GetLabelFullName(target, targetAsmLoad);
                var label = targetAsmLoad.Scope.Labels.Where(m => string.Compare(m.LabelFullName, labelFullName, true) == 0).FirstOrDefault();
                if (label != default)
                {
                    return label;
                }
                targetAsmLoad = targetAsmLoad.ParentAsmLoad;
            }
            return default;
        }

        public Function FindFunction(string target)
        {
            var targetAsmLoad = this;

            while (targetAsmLoad != default)
            {
                var longFunctionName = Function.GetFunctionFullName(target, targetAsmLoad);
                var function = targetAsmLoad.Scope.Functions.Where(m => string.Compare(m.FullName, longFunctionName, true) == 0).FirstOrDefault();
                if (function != default)
                {
                    return function;
                }
                targetAsmLoad = targetAsmLoad.ParentAsmLoad;
            }
            return default;
        }

        public Macro FindMacro(string target)
        {
            var targetAsmLoad = this;

            while (targetAsmLoad != default)
            {
                var longMacroName = Macro.GetMacroFullName(target, targetAsmLoad);
                var function = targetAsmLoad.Scope.Macros.Where(m => string.Compare(m.FullName, longMacroName, true) == 0).FirstOrDefault();
                if (function != default)
                {
                    return function;
                }
                targetAsmLoad = targetAsmLoad.ParentAsmLoad;
            }
            return default;
        }

        /// <summary>
        /// 出力のスペース領域を埋める
        /// </summary>
        /// <param name="fromOutputAddress"></param>
        /// <param name="toOutputAddress"></param>
        /// <param name="stream"></param>
        /// <exception cref="NotSupportedException"></exception>
        public void OutputORGSpace(ref UInt32 fromOutputAddress, UInt32 toOutputAddress, Stream stream)
        {
            var outputAddress = fromOutputAddress;
            var asmORGs = this.Share.AsmORGs.OrderBy(m => m.OutputAddress).ToList();
            var startORG = asmORGs.Where(m => m.OutputAddress <= outputAddress).OrderBy(m => m.OutputAddress).LastOrDefault();
            var endORG = asmORGs.Where(m => m.OutputAddress <= toOutputAddress).OrderBy(m => m.OutputAddress).LastOrDefault();
            var startIndex = asmORGs.IndexOf(startORG);
            var endIndex = asmORGs.IndexOf(endORG);

            if (startIndex == -1 || endIndex == -1)
            {
                throw new NotSupportedException();
            }

            for (var index = startIndex; index <= endIndex; index++)
            {
                var length = default(UInt32);
                if (index < endIndex)
                {
                    length = asmORGs[index + 1].OutputAddress - outputAddress;
                }
                else
                {
                    length = toOutputAddress - outputAddress;

                }
                var bytes = Enumerable.Repeat<byte>(asmORGs[index].FillByte, (int)length).ToArray();
                stream.Write(bytes, 0, bytes.Length);
                outputAddress += length;
            }

            fromOutputAddress = outputAddress;
        }

        public FileInfo FindPramgaOnceFile(FileInfo fileInfo)
        {
            return this.Share.PragmaOnceFiles.FirstOrDefault(m => m.GetFullNameCaseSensitivity() == fileInfo.GetFullNameCaseSensitivity());
        }


        public AsmEnum.EncodeModeEnum GetEncodMode(FileInfo fileInfo)
        {
            var encodeMode =　AssembleOption.InputEncodeMode;
            if (encodeMode == AsmEnum.EncodeModeEnum.AUTO)
            {
                encodeMode = AssembleOption.CheckEncodeMode(fileInfo);
            }
            return encodeMode;
        }

        public System.Text.Encoding GetInputEncoding(FileInfo fileInfo)
        {
            var encodeMode = GetEncodMode(fileInfo);
            var encoding = GetInputEncoding(encodeMode);

            return encoding;
        }

        public System.Text.Encoding GetInputEncoding(AsmEnum.EncodeModeEnum encodeMode)
        {
            if (encodeMode == AsmEnum.EncodeModeEnum.AUTO)
            {
                throw new ArgumentException("encodeMode:AUTOは指定できません");
            }

            AssembleOption.OutputEncodeMode = encodeMode;
            var encoding = GetEncoding(encodeMode);

            return encoding;
        }


        public System.Text.Encoding GetOutputEncoding()
        {
            var encoding = GetEncoding(AssembleOption.OutputEncodeMode);
            return encoding;
        }

        /// <summary>
        /// エンコードモードにしたがって、.NETのEncodingを返す
        /// </summary>
        /// <param name="encodeMode"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static System.Text.Encoding GetEncoding(AsmEnum.EncodeModeEnum encodeMode)
        {
            var encoding = encodeMode switch
            {
                AsmEnum.EncodeModeEnum.UTF_8 => AIEncode.GetEncodingUTF8(),
                AsmEnum.EncodeModeEnum.SHIFT_JIS => AIEncode.GetEncodingSJIS(),
                _ => throw new NotImplementedException()
            };

            return encoding;
        }

        /// <summary>
        /// アウトプットモードのファイルタイプを返す
        /// </summary>
        /// <param name="outputMode"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static AsmEnum.FileDataTypeEnum GetFileType(AsmEnum.FileTypeEnum fileType)
        {
            var dataType = fileType switch
            {
                AsmEnum.FileTypeEnum.BIN => AsmEnum.FileDataTypeEnum.Binary,
                AsmEnum.FileTypeEnum.HEX => AsmEnum.FileDataTypeEnum.Text,
                AsmEnum.FileTypeEnum.T88 => AsmEnum.FileDataTypeEnum.Binary,
                AsmEnum.FileTypeEnum.CMT => AsmEnum.FileDataTypeEnum.Binary,
                AsmEnum.FileTypeEnum.LST => AsmEnum.FileDataTypeEnum.Text,
                AsmEnum.FileTypeEnum.SYM => AsmEnum.FileDataTypeEnum.Text,
                AsmEnum.FileTypeEnum.EQU => AsmEnum.FileDataTypeEnum.Text,
                AsmEnum.FileTypeEnum.DBG => AsmEnum.FileDataTypeEnum.Text,
                _ => throw new NotImplementedException()
            };

            return dataType;
        }

        public void OutputLabels(StreamWriter streamWriter)
        {
            var globalLabels = this.Scope.Labels.GroupBy(m => m.GlobalLabelName).Select(m => m.Key);
            var globalLabelMode = globalLabels.Count() > 1;

            foreach (var globalLabelName in globalLabels)
            {
                if (globalLabelMode)
                {
                    streamWriter.WriteLine($"[{globalLabelName}]");
                }
                foreach (var label in this.Scope.Labels.Where(m => m.DataType == Label.DataTypeEnum.Value && m.GlobalLabelName == globalLabelName))
                {
                    streamWriter.WriteLine($"{label.Value.ConvertTo<UInt16>():X4} {label.LabelShortName}");
                }
                streamWriter.WriteLine();
            }

            if (globalLabelMode)
            {
                foreach (var label in this.Scope.Labels.Where(m => m.DataType == Label.DataTypeEnum.Value))
                {
                    streamWriter.WriteLine($"{label.Value.ConvertTo<UInt16>():X4} {label.LabelFullName}");
                }
            }
        }

        public void OutputEqualLabels(StreamWriter streamWriter)
        {
            OutputEqualAddrLabels(streamWriter, false);
        }

        public void OutputAddrLabels(StreamWriter streamWriter)
        {
            OutputEqualAddrLabels(streamWriter, true);
        }

        public void OutputEqualAddrLabels(StreamWriter streamWriter, bool addressMode)
        {
            var labelMaxLength = this.AssembleOption.TabSize;
            var globalLabels = this.Scope.Labels.GroupBy(m => m.GlobalLabelName).Select(m => m.Key);
            var globalLabelMode = globalLabels.Count() > 1;
            streamWriter.WriteLine("#pragma once");
            streamWriter.WriteLine();

            foreach (var globalLabelName in globalLabels)
            {
                if (globalLabelMode)
                {
                    streamWriter.WriteLine();
                    streamWriter.WriteLine($"[{globalLabelName}]");
                    streamWriter.WriteLine();
                }

                var labels = this.Scope.Labels.Where(m => m.DataType == Label.DataTypeEnum.Value && m.GlobalLabelName == globalLabelName);
                // addressModeで出し分けをする
                var equLabels = labels.Where(m => (m.LabelLevel == Label.LabelLevelEnum.Label && (m.LabelType == Label.LabelTypeEnum.Equ || (!addressMode && m.LabelType == Label.LabelTypeEnum.Adr))));
                // addressModeがTrueの時だけ使う
                var orgLabels = labels.Where(m => addressMode && m.LabelType == Label.LabelTypeEnum.Adr).OrderBy(m => m.Value.ConvertTo<int>()).Select(m => m.Value).Distinct();

                // ラベルの最大長を求める
                labelMaxLength = labels.Max(m => m.LabelName.Length + 2);
                                 
                // EQU
                foreach (var label in equLabels)
                {
                    var labelName = $"{label.LabelName}:";
                    var equValue = $"${label.Value.ConvertTo<UInt16>():X4}";
                    if (AIMath.TryParse(label.Value.OriginalValue, out var tmpAIValue) &&
                        label.Value.Equals(tmpAIValue))
                    {
                        equValue = label.Value.OriginalValue;
                    }
                    streamWriter.WriteLine($"{labelName.PadRight(labelMaxLength)}equ {equValue}");

                    // sub equ
                    foreach (var item in labels.Where(m => m.LabelName == label.LabelName && m.LabelLevel == Label.LabelLevelEnum.SubLabel && (m.LabelType == Label.LabelTypeEnum.Equ || (!addressMode && m.LabelType == Label.LabelTypeEnum.Adr))))
                    {
                        var subLabelName = $".{item.SubLabelName}";
                        var subEquValue = $"${item.Value.ConvertTo<UInt16>():X4}";
                        if (AIMath.TryParse(item.Value.OriginalValue, out var subTmpAIValue) &&
                            item.Value.Equals(subTmpAIValue))
                        {
                            subEquValue = item.Value.OriginalValue;
                        }
                        streamWriter.WriteLine($"{subLabelName.PadRight(labelMaxLength)}equ {subEquValue}");
                    }
                }
                if (globalLabels.Any())
                { 
                    streamWriter.WriteLine();
                }

                // ORG
                var saveAddress = int.MaxValue;
                foreach (var address in orgLabels.Select(m => m.ConvertTo<int>()))
                {
                    foreach (var label in labels.Where(m => m.Value.ConvertTo<int>() == address && m.LabelLevel == Label.LabelLevelEnum.Label && m.LabelType == Label.LabelTypeEnum.Adr))
                    {
                        if (saveAddress != address)
                        {
                            // ORG
                            streamWriter.WriteLine();
                            streamWriter.WriteLine($"{new string(' ', labelMaxLength)}org ${address:X4}");
                            saveAddress = address;
                        }

                        // Add Label
                        var labelName = $"{label.LabelName}:";
                        streamWriter.WriteLine($"{labelName.PadRight(labelMaxLength)}");

                        // sub equ
                        foreach (var item in labels.Where(m => m.LabelName == label.LabelName && m.LabelLevel == Label.LabelLevelEnum.SubLabel && m.LabelType == Label.LabelTypeEnum.Equ))
                        {
                            var subLabelName = $".{item.SubLabelName}";
                            var equValue = $"${item.Value.ConvertTo<UInt16>():X4}";
                            if (AIMath.TryParse(item.Value.OriginalValue, out var tmpValue) && item.Value.Equals(tmpValue))
                            {
                                equValue = item.Value.OriginalValue;
                            }

                            streamWriter.WriteLine($"{subLabelName.PadRight(labelMaxLength)}equ {equValue} ");
                        }

                        // SubAddress
                        foreach (var item in labels.Where(m => m.LabelName == label.LabelName && m.LabelLevel == Label.LabelLevelEnum.SubLabel && m.LabelType == Label.LabelTypeEnum.Adr))
                        {
                            var itemAddress = item.Value.ConvertTo<int>();
                            if (saveAddress != itemAddress)
                            {
                                // ORG
                                streamWriter.WriteLine();
                                streamWriter.WriteLine($"{new string(' ', labelMaxLength)}org ${address:X4}");
                                saveAddress = itemAddress;
                            }

                            var subLabelName = $".{item.SubLabelName}";
                            streamWriter.WriteLine($"{subLabelName.PadRight(16)}");
                        }
                    }
                }
                if (orgLabels.Any())
                {
                    streamWriter.WriteLine();
                }
            }
        }
        
    }
}
