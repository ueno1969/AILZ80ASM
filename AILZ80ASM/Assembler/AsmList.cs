﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AILZ80ASM.Assembler
{
    public class AsmList
    {
        public enum NestedCodeTypeEnum
        {
            Macro,
            Repeat,
        }

        public enum ListStatusEnum
        {
            Normal,
            SourceOnly,
        }

        public UInt32? OutputAddress { get; set; }
        public UInt32? ProgramAddress { get; set; }
        public byte[] Bin { get; set; }
        public string Status { get; set; }
        public Stack<NestedCodeTypeEnum> NestedCodeTypes { get; set; }
        public string Source { get; set; }
        public int OutputLineIndex { get; set; }
        public int OutputLineCount { get; set; }
        public ListStatusEnum ListStatus { get; set; }
        public Error.ErrorCodeEnum? ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public bool IsBinaryFile { get; set; }

        private AsmList()
        {
        }

        /*
        // 必要になったら有効にする
        public static AsmList CreateFileInfoBOF(FileInfo fileInfo, AsmEnum.EncodeModeEnum encodeMode)
        {
            return CreateSourceOnly($"[BOF:{fileInfo.Name}:{encodeMode}]");
        }
        */

        public static AsmList CreateFileInfoEOF(FileInfo fileInfo, int length)
        {
            return CreateSourceOnly($"[EOF:{fileInfo.Name}:{length}]");
        }

        public static AsmList CreateFileInfoEOF(FileInfo fileInfo, AsmEnum.EncodeModeEnum encodeMode)
        {
            return CreateSourceOnly($"[EOF:{fileInfo.Name}:{encodeMode}]");
        }

        public static AsmList CreateSourceOnly(string target)
        {
            return Create(default(UInt32?), default(UInt32?), default(Error.ErrorCodeEnum?), "", default(byte[]), "", target, false, ListStatusEnum.SourceOnly);
        }

        public static AsmList CreateSource(string target)
        {
            return Create(default(UInt32?), default(UInt32?), default(Error.ErrorCodeEnum?), "", default(byte[]), "", target, false, ListStatusEnum.Normal);
        }

        public static AsmList CreateSource(string target, Error.ErrorCodeEnum? errorCode, string errorMessage)
        {
            return Create(default(UInt32?), default(UInt32?), errorCode, errorMessage, default(byte[]), "", target, false, ListStatusEnum.Normal);
        }

        public static AsmList CreateLineItem(LineItem lineItem)
        {
            return CreateSource(lineItem.LineString, lineItem?.ErrorLineItem?.ErrorCode, lineItem?.ErrorLineItem?.ErrorMessage);
        }

        public static AsmList CreateLineItem(LineItem lineItem, AsmAddress asmAddress)
        {
            return Create(default(UInt32?), asmAddress.Program, lineItem?.ErrorLineItem?.ErrorCode, lineItem?.ErrorLineItem?.ErrorMessage, default(byte[]), "", lineItem.LineString, false, ListStatusEnum.Normal);
        }

        public static AsmList CreateLineItemCommentOut(LineItem lineItem)
        {
            return CreateSource($";{lineItem.LineString}", lineItem?.ErrorLineItem?.ErrorCode, lineItem?.ErrorLineItem?.ErrorMessage);
        }

        public static AsmList CreateLineItemEqual(Label equLabel, LineItem lineItem)
        {
            var programAddress = default(UInt32?);

            if (equLabel != default &&
                equLabel.DataType == Label.DataTypeEnum.Value && equLabel.Value.ValueType.HasFlag(AILight.AIValue.ValueTypeEnum.Int32))
            {
                programAddress = equLabel.Value.ConvertTo<UInt32>();
            }
            
            return CreateLineItem(default(UInt32?), programAddress, default(byte[]), "", lineItem);
        }

        public static AsmList CreateLineItemEnum(UInt32 value, LineItem lineItem)
        {
            var programAddress = default(UInt32?);

            programAddress = value;

            return CreateLineItem(default(UInt32?), programAddress, default(byte[]), "", lineItem);
        }

        public static AsmList CreateLineItemEnd(LineItem lineItem)
        {
            return CreateLineItem(default(UInt32?), default(UInt32?), default(byte[]), "", lineItem);
        }

        public static AsmList CreateLineItemEndDefine(UInt16? entryPoint, LineItem lineItem)
        {
            return CreateLineItem(default(UInt32?), entryPoint, default(byte[]), "", lineItem);
        }

        public static AsmList CreateLineItemORG(AsmAddress address, AsmLength length, LineItem lineItem)
        {
            return CreateLineItem(new AsmAddress(address, length), default(byte[]), "", lineItem);
        }

        public static AsmList CreateLineItemBinaryFile(AsmAddress asmAdddress, Error.ErrorCodeEnum? errorCode, string errorMessage, byte[] bin)
        {
            return Create(asmAdddress.Output, asmAdddress.Program, errorCode, errorMessage, bin, "", "", true, ListStatusEnum.Normal);
        }

        public static AsmList CreateLineItem(AsmAddress asmAdddress, Error.ErrorCodeEnum? errorCode, string errorMessage, byte[] bin)
        {
            return Create(asmAdddress.Output, asmAdddress.Program, errorCode, errorMessage, bin, "", "", false, ListStatusEnum.Normal);
        }

        public static AsmList CreateLineItem(AsmAddress asmAdddress, byte[] bin, string status, LineItem lineItem)
        {
            return CreateLineItem(asmAdddress.Output, asmAdddress.Program, bin, status, lineItem);
        }

        public static AsmList CreateLineItem(UInt32? outputAddress, UInt32? programAddress, byte[] bin, string status, LineItem lineItem)
        {
            return Create(outputAddress, programAddress, lineItem?.ErrorLineItem?.ErrorCode, lineItem?.ErrorLineItem?.ErrorMessage, bin, status, lineItem.LineString, false, ListStatusEnum.Normal);
        }

        public static AsmList Create(UInt32? outputAddress, UInt32? programAddress, Error.ErrorCodeEnum? errorCode, string errorMessage, byte[] bin, string status, string source, bool binaryFile, ListStatusEnum listStatus)
        {
            return new AsmList
            {
                OutputAddress = outputAddress,
                ProgramAddress = programAddress,
                ErrorCode = errorCode,
                ErrorMessage = errorMessage,
                Bin = bin,
                Status = status,
                Source = source,
                ListStatus = listStatus,
                OutputLineIndex = 0,
                IsBinaryFile = binaryFile
            };
        }

        public override string ToString()
        {
            return ToString(AsmEnum.ListFormatEnum.Full, 4, false);
        }

        public string ToString(AsmEnum.ListFormatEnum listFormat, int tabSize, bool listOmitBinaryFile)
        {
            var address1 = OutputAddress.HasValue ? $"{OutputAddress:X6}" : "";
            var address2 = "";
            if (ProgramAddress.HasValue && 
                ProgramAddress.Value > UInt16.MaxValue &&
               (ProgramAddress.Value & 0xFFFF8000) != 0xFFFF8000)
            {
                if (string.IsNullOrEmpty(address1) && ProgramAddress <= 0xFFFFFF)
                {
                    address1 = $"    {ProgramAddress / 0x10000:X2}";
                    address2 = $"{ProgramAddress & 0xFFFF:X4}";
                }
                else
                {
                    address1 = $"    --";
                    address2 = $"{ProgramAddress & 0xFFFF:X4}";
                }
            }
            else
            {
                address2 = ProgramAddress.HasValue ? $"{(ProgramAddress & 0xFFFF):X4}" : "";
            }
            var binary = Bin != default ? string.Concat(Bin.Select(m => $"{m:X2}")) : "";
            var codeType = "";
            var status = this.Status;
            var source = GetReplaseTab(this.Source, tabSize);
            var binaryLength = listFormat switch
            {
                AsmEnum.ListFormatEnum.Simple => 14,
                _ => 16,
            };

            if (ErrorCode.HasValue && Error.GetErrorType(ErrorCode.Value) == Error.ErrorTypeEnum.Error)
            {
                binary = $"**** {ErrorCode} ****";
            }
            else if (this.Bin != default && this.Bin.Length > binaryLength)
            {
                var startBin = this.Bin[0];
                if (this.Bin.Count(m => m == startBin) == this.Bin.Length)
                {
                    // 同一の値を出力するときに、省略表示する
                    binary = $"{startBin:X2} LEN:{this.Bin.Length}";
                }
                else if (listOmitBinaryFile && binary.Length > binaryLength)
                {
                    var lenString = $"LEN:{this.Bin.Length}";
                    var lenSpace = lenString.Length % 2 == 0 ? "" : " ";
                    lenString = $"..{lenSpace}{lenString}";
                    if (lenString.Length <= binaryLength)
                    {
                        binary = binary.Substring(0, binaryLength - lenString.Length) + lenString;
                    }
                    else
                    {
                        binary = binary.Substring(0, binaryLength - 2) + $"..LEN:{this.Bin.Length}";
                    }
                }
            }
            if (NestedCodeTypes != default && NestedCodeTypes.Count > 0)
            {
                codeType = "+";
            }

            var results = new List<string>();

            switch (this.ListStatus)
            {
                case ListStatusEnum.Normal:
                    foreach (var item in Regex.Split(binary, @"(?<=\G.{" + binaryLength.ToString() + "})(?!$)"))
                    {
                        address1 = address1.PadLeft(6);
                        address2 = address2.PadLeft(4);
                        status = status.PadLeft(2);
                        codeType = codeType.PadLeft(1);
                        var binaryString = item.PadRight(binaryLength);

                        switch (listFormat)
                        {
                            case AsmEnum.ListFormatEnum.Simple:
                                results.Add($"  {address2}  {binaryString} {codeType}{source}");
                                break;
                            case AsmEnum.ListFormatEnum.Middle:
                                results.Add($"{address2} {binaryString}{status} {codeType}{source}");
                                break;
                            case AsmEnum.ListFormatEnum.Full:
                                results.Add($"{address1} {address2} {binaryString}{status} {codeType}{source}");
                                break;
                            default:
                                break;
                        }

                        // クリアする
                        address1 = "";
                        address2 = "";
                        status = "";
                        source = "";
                    }

                    break;
                case ListStatusEnum.SourceOnly:
                    results.Add(source);
                    break;
                default:
                    break;
            }

            OutputLineCount = results.Count == 0 ? 1 : results.Count; //出力行を設定

            return string.Join(Environment.NewLine, results);
        }

        /// <summary>
        /// 展開されるコードのタイプを設定
        /// </summary>
        /// <param name="codeType"></param>
        public void PushNestedCodeType(NestedCodeTypeEnum nestedCodeTypeEnum)
        {
            if (NestedCodeTypes == default)
            {
                NestedCodeTypes = new Stack<NestedCodeTypeEnum>();
            }

            NestedCodeTypes.Push(nestedCodeTypeEnum);
        }

        private static string GetReplaseTab(string target, int tabLength)
        {
            var result = new List<char>();
            var counter = 0;

            foreach (var item in target.ToArray())
            {
                if (item == '\t')
                {
                    var spaceCount = tabLength;
                    if (spaceCount != counter)
                    {
                        spaceCount -= counter;
                    }
                    result.AddRange(new string(' ', spaceCount).ToArray());
                    counter = 0;
                }
                else if (item <= 0x7F)
                {
                    result.Add(item);
                    counter += 1;
                }
                else
                {
                    result.Add(item);
                    counter += 2;
                }

                if (counter > tabLength)
                {
                    counter -= tabLength;
                }
            }

            return string.Concat(result);
        }

    }
}
