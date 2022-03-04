﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AILZ80ASM.InstructionSet;
using AILZ80ASM.Exceptions;
using AILZ80ASM.AILight;

namespace AILZ80ASM.Assembler
{
    public class AsmLoad
    {
        public ISA ISA { get; private set; } // 命令セット

        // 実行ラベル
        public string GlobalLabelName { get; private set; }
        public string LabelName { get; private set; }

        // アセンブル終了フラグ
        public bool AsmEnd { get; set; } = false;

        // デフォルトキャラクターマップ
        public string DefaultCharMap { get; set; }

        // 循環展開確認用
        public Stack<FileInfo> LoadFiles { get; private set; } = new Stack<FileInfo>();
        public Stack<Macro> LoadMacros { get; private set; } = new Stack<Macro>();

        // 展開判断用
        public LineDetailItem LineDetailItemForExpandItem { get; set; } = null;

        // 出力制御用
        public List<OperationItem> TirmOperationItems { get; private set; } = new List<OperationItem>(); // トリム用アイテムにマーク
        public List<AsmAddress> AsmAddresses { get; private set; } = new List<AsmAddress>();

        // アセンブルオプション
        public AsmOption AssembleOption { get; private set; }

        public ErrorLineItem[] AssembleErrors
        {
            get
            {
                return Errors.Where(m => m.ErrorType == Error.ErrorTypeEnum.Error && (AssembleOption.DisableWarningCodes == default || !AssembleOption.DisableWarningCodes.Any(n => m.ErrorCode == n))).ToArray();
            }
        }

        public ErrorLineItem[] AssembleWarnings
        {
            get
            {
                return Errors.Where(m => m.ErrorType == Error.ErrorTypeEnum.Warning && (AssembleOption.DisableWarningCodes == default || !AssembleOption.DisableWarningCodes.Any(n => m.ErrorCode == n))).ToArray();
            }
        }

        public ErrorLineItem[] AssembleInformation
        {
            get
            {
                return Errors.Where(m => m.ErrorType == Error.ErrorTypeEnum.Information && (AssembleOption.DisableWarningCodes == default || !AssembleOption.DisableWarningCodes.Any(n => m.ErrorCode == n))).ToArray();
            }
        }

        // 制御、マクロ用
        private List<Label> Labels { get; set; } = default;
        private List<Macro> Macros { get; set; } = default;
        private List<Function> Functions { get; set; } = default;
        private List<ErrorLineItem> Errors { get; set; } = default;
        // ネームスペースの保存
        private List<string> GlobalLabelNames { get; set; } = default;
        // スコープの親
        private AsmLoad ParentAsmLoad { get; set; } = default;

        public AsmLoad(ISA isa, AsmOption assembleOption)
        {
            ISA = isa;

            Labels = new List<Label>();
            Macros = new List<Macro>();
            Functions = new List<Function>();
            Errors = new List<ErrorLineItem>();
            GlobalLabelNames = new List<string>();
            AssembleOption = assembleOption;
        }

        /// <summary>
        /// クローン
        /// </summary>
        /// <returns></returns>
        public AsmLoad Clone()
        {
            var clone = InternalClone();

            clone.ParentAsmLoad = this.ParentAsmLoad;

            clone.Labels = this.Labels;
            clone.Functions = this.Functions;
            clone.Macros = this.Macros;
            clone.GlobalLabelNames = this.GlobalLabelNames;

            return clone;
        }

        /// <summary>
        /// 新しいスコープのクローン
        /// </summary>
        /// <param name="globalLabelName"></param>
        /// <returns></returns>
        public AsmLoad CloneWithNewScore(string globalLabelName, string labelName)
        {
            var clone = InternalClone();
            clone.ParentAsmLoad = this;
            clone.GlobalLabelName = globalLabelName;
            clone.LabelName = labelName;

            clone.Labels = new List<Label>();
            clone.Functions = new List<Function>();
            clone.Macros = new List<Macro>();
            clone.GlobalLabelNames = new List<string>();
            clone.GlobalLabelNames.Add(globalLabelName);

            return clone;
        }

        private AsmLoad InternalClone()
        {
            var asmLoad = new AsmLoad(this.ISA, this.AssembleOption)
            {
                GlobalLabelName = this.GlobalLabelName,
                LabelName = this.LabelName,
                AsmAddresses = this.AsmAddresses,
                LoadFiles = this.LoadFiles,
                LoadMacros = this.LoadMacros,
                LineDetailItemForExpandItem = this.LineDetailItemForExpandItem,

                Errors = this.Errors,
                TirmOperationItems = this.TirmOperationItems,
                DefaultCharMap = this.DefaultCharMap,
                AsmEnd = this.AsmEnd,
            };

            return asmLoad;
        }

        /// <summary>
        /// クローン後にスコープが変わったものを戻す
        /// </summary>
        /// <param name="asmLoad"></param>
        public void SetScope(AsmLoad asmLoad)
        {
            this.GlobalLabelName = asmLoad.GlobalLabelName;
            this.LabelName = asmLoad.LabelName;
            this.AsmEnd = asmLoad.AsmEnd;
        }

        public void LoadCloseValidate()
        {
            if (this.LineDetailItemForExpandItem is LineDetailItemMacroDefine)
            {
                this.AddError(new ErrorLineItem(LineDetailItemForExpandItem.LineItem, Error.ErrorCodeEnum.E3001));
            }

            if (this.LineDetailItemForExpandItem is LineDetailItemRepeat)
            {
                this.AddError(new ErrorLineItem(LineDetailItemForExpandItem.LineItem, Error.ErrorCodeEnum.E1011));
            }

            if (this.LineDetailItemForExpandItem is LineDetailItemConditional)
            {
                this.AddError(new ErrorLineItem(LineDetailItemForExpandItem.LineItem, Error.ErrorCodeEnum.E1021));
            }
        }

        /*
        public string GetlFullName(string target)
        {
            return Label.GetLabelFullName(target, this);
        }

        public string GetFunctionFullName(string target)
        {
            return Function.GetFunctionFullName(target, this);
        }

        public string GetMacroFullName(string target)
        {
            return Macro.GetMacroFullName(target, this);
        }
        */

        public void AddError(ErrorLineItem errorLineItem)
        {
            Errors.Add(errorLineItem);
        }

        public void AddErrors(IEnumerable<ErrorLineItem> errorLineItems)
        {
            Errors.AddRange(errorLineItems);
        }

        public void AddLabel(Label label)
        {
            // 同一名のラベル
            if (this.Labels.Any(m => string.Compare(m.LabelFullName, label.LabelFullName, true) == 0))
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E0014);
            }
            if (label.LabelLevel == Label.LabelLevelEnum.GlobalLabel)
            {
                // ラベルと同じ名前は付けられない
                if (this.Labels.Any(m => string.Compare(m.LabelName, label.LabelFullName, true) == 0))
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0017);
                }

                if (!this.GlobalLabelNames.Any(m => string.Compare(m, label.LabelFullName, true) == 0))
                {
                    // ネームスペースが変わるときには保存する
                    this.GlobalLabelNames.Add(label.LabelFullName);
                }
                this.GlobalLabelName = label.GlobalLabelName;
                this.LabelName = label.LabelName;
            }
            else
            {
                // ネームスペースとと同じ名前は付けられない
                if (this.GlobalLabelNames.Any(m => string.Compare(m, label.LabelName, true) == 0))
                {
                    throw new ErrorAssembleException(Error.ErrorCodeEnum.E0018);
                }
                this.Labels.Add(label);
                this.LabelName = label.LabelName;
            }

        }

        public void AddFunction(Function function)
        {
            if (this.Functions.Any(m => string.Compare(m.FullName, function.FullName, true) == 0))
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E4001);
            }
            this.Functions.Add(function);
        }

        public void AddMacro(Macro macro)
        {
            if (this.Macros.Any(m => string.Compare(m.FullName, macro.FullName, true) == 0))
            {
                throw new ErrorAssembleException(Error.ErrorCodeEnum.E3010);
            }
            this.Macros.Add(macro);
        }

        public string FindGlobalLabelName(string target)
        {
            var targetAsmLoad = this;

            while (targetAsmLoad != default)
            {
                var name = targetAsmLoad.GlobalLabelNames.Where(m => string.Compare(m, target, true) == 0).FirstOrDefault();
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
                var label = targetAsmLoad.Labels.Where(m => string.Compare(m.LabelFullName, labelFullName, true) == 0).FirstOrDefault();
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
                var function = targetAsmLoad.Functions.Where(m => string.Compare(m.FullName, longFunctionName, true) == 0).FirstOrDefault();
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
                var function = targetAsmLoad.Macros.Where(m => string.Compare(m.FullName, longMacroName, true) == 0).FirstOrDefault();
                if (function != default)
                {
                    return function;
                }
                targetAsmLoad = targetAsmLoad.ParentAsmLoad;
            }
            return default;
        }

        public void AddAsmAddress(AsmAddress asmAddress)
        {
            AsmAddresses.Add(asmAddress);
        }

        public void AddTrimOperationItem(OperationItem operationItem)
        {
            TirmOperationItems.Add(operationItem);
        }

        public void ClearTrimOperationItem()
        {
            TirmOperationItems.Clear();
        }


        public AsmEnum.EncodeModeEnum GetEncodMode(FileInfo fileInfo)
        {
            var encodeMode =　AssembleOption.InputEncodeMode;
            if (encodeMode == AsmEnum.EncodeModeEnum.AUTO)
            {
                using var readStream = fileInfo.OpenRead();
                using var memoryStream = new MemoryStream();
                readStream.CopyTo(memoryStream);
                var bytes = memoryStream.ToArray();

                var isUTF8 = AIEncode.IsUTF8(bytes);
                var isSHIFT_JIS = AIEncode.IsSHIFT_JIS(bytes);
                encodeMode = AsmEnum.EncodeModeEnum.UTF_8;
                if (!isUTF8 && isSHIFT_JIS)
                {
                    encodeMode = AsmEnum.EncodeModeEnum.SHIFT_JIS;
                }
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
                AsmEnum.FileTypeEnum.DBG => AsmEnum.FileDataTypeEnum.Text,
                _ => throw new NotImplementedException()
            };

            return dataType;
        }

        public void OutputLabels(Stream stream)
        {
            using var streamWriter = new StreamWriter(stream);

            foreach (var label in Labels.Where(m => !m.Invalidate))
            {
                streamWriter.WriteLine($"{label.Value:X4} {label.LabelName}");
            }
            foreach (var label in Labels.Where(m => !m.Invalidate))
            {
                streamWriter.WriteLine($"{label.Value:X4} {label.LabelFullName}");
            }
        }
    }
}
