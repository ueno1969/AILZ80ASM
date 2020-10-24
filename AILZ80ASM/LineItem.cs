﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace AILZ80ASM
{
    public class LineItem
    {
        private string LineString { get; set; }
        private int LineIndex { get; set; }
        internal FileItem FileItem { get; set; }
        /* Configクラスへ移行
        private const int MAX_INCLUDE_NEST = 10;
        */

        public string OperationString { get; private set; }
        public string CommentString { get; private set; }
        //public Macro Macro { get; private set; }
        public Lable Label { get; private set; }
        public IOperationItem OperationItem { get; private set; }
        public UInt16 Address { get; private set; }


        public byte[] Bin 
        {
            get 
            {
                return OperationItem == default(IOperationItem) ? new byte[] { } : OperationItem.Bin;
            } 
        }

        public LineItem(string lineString, int lineIndex, FileItem fileItem)
        {
            LineString = lineString;
            LineIndex = lineIndex;
            FileItem = fileItem;

            OperationItem = default(IOperationItem);

            //コメントを処理する
            var indexCommnet = lineString.IndexOf(';');
            if (indexCommnet != -1)
            {
                CommentString = lineString.Substring(indexCommnet);
                lineString = lineString.Substring(0, indexCommnet);
            }

            //命令を切り出す
            OperationString = lineString.TrimEnd();

            // ラベルを処理する
            Label = new Lable(this);
        }

        public void PreAssemble(ref UInt16 address)
        {
            // Addressを設定
            Address = address;

            // 命令を判別する
            OperationItem = OperationItem ?? OperationItemOPCode.Perse(this);　// OpeCode
            OperationItem = OperationItem ?? OperationItemInclude.Perse(this); // Include
            OperationItem = OperationItem ?? OperationItemSystem.Perse(this); // System

            // ラベルを設定する
            //Label.

            // Addressを返す
            address = OperationItem.NextAddress;
        }

        public void Assemble()
        {
            OperationItem.Assemble();
        }

            /*
            public void PreProcess()
            {

                PreProcess(0);
            }

            public void PreProcess(int level)
            {
                // includeを処理する
                // include "Macro.inc"
                var matched = Regex.Match(OperationString, @"^include\s*""(?<include>.*)"".*", RegexOptions.Singleline);

                if (matched.Success)
                {
                    if (level > MAX_INCLUDE_NEST)
                    {
                        throw new Exception($"Includeのネストは{MAX_INCLUDE_NEST}段までです");
                    }

                    var filePathString = matched.Groups["include"].Value;
                    IncludeFileItem = new FileItem(new FileInfo(filePathString));
                    IncludeFileItem.PreProcess(level + 1);
                }
                else
                {
                    OperationItem = new OperationItem(OperationString);
                }
            }
            */

            public void SetLabel(ref ushort address, ref string nameSpace, IList<Lable> labelList)
        {
            /*
            if (!string.IsNullOrEmpty(LabelString))
            {
                if (LabelString[0] == '.')
                {
                    var tmpNameSpace = nameSpace;
                    var label = labelList.Last(_ => _.NameSpace == tmpNameSpace && _.LocalLabelName == "");
                    labelList.Add(new Lable { NameSpace = nameSpace, LabelName = LabelString, LocalLabelName = LabelString.Substring(1), DataLength = Lable.DataLengthEnum.DW, Value = address });
                }
                else
                {
                    labelList.Add(new Lable { NameSpace = nameSpace, LabelName = LabelString.Substring(0, LabelString.Length - 1), LocalLabelName = "", DataLength = Lable.DataLengthEnum.DW, Value = address });
                }
            }

            if (!string.IsNullOrEmpty(MnemonicString))
            {
                var opCodeItem = OPCodeTable.GetOPCodeItem(MnemonicString, null);
                switch (opCodeItem.OPCodeStatus)
                {
                    case OPCodeResult.OPCodeStatusEnum.ORG:
                        address = opCodeItem.Address;
                        break;
                    case OPCodeResult.OPCodeStatusEnum.OP:
                    case OPCodeResult.OPCodeStatusEnum.DATA:
                        address += (byte)opCodeItem.OPCode.Length;
                        break;
                    case OPCodeResult.OPCodeStatusEnum.ERROR:
                        break;
                    default:
                        break;
                }
            }
            */
        }

        public void Assemble(ref ushort address, Lable[] labelList)
        {
            /*
            if (!string.IsNullOrEmpty(MnemonicString))
            {
                Address = address;
                var opCodeItem = OPCodeTable.GetOPCodeItem(MnemonicString, labelList);
                switch (opCodeItem.OPCodeStatus)
                {
                    case OPCodeResult.OPCodeStatusEnum.ORG:
                        address = opCodeItem.Address;
                        break;
                    case OPCodeResult.OPCodeStatusEnum.OP:
                    case OPCodeResult.OPCodeStatusEnum.DATA:
                        address += (byte)opCodeItem.OPCode.Length;
                        Bin = opCodeItem.ToBin();
                        break;
                    case OPCodeResult.OPCodeStatusEnum.ERROR:
                        break;
                    default:
                        break;
                }
            }
            */
        }


    }
}
