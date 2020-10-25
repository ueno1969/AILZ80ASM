﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AILZ80ASM
{
    public class Package
    {
        private List<FileItem> FileItems { get; set; } = new List<FileItem>();

        public Package(FileInfo[] Files)
        {
            foreach (var fileInfo in Files)
            {
                FileItems.Add(new FileItem(fileInfo, this));
            }
        }

        public void Assemble()
        {
            var address = default(UInt16);
            var labels = new Label[] { };

            foreach (var fileItem in FileItems)
            {
                fileItem.PreAssemble(ref address);
            }

            foreach (var fileItem in FileItems)
            {
                fileItem.SetValueLabel(labels);
            }

            foreach (var fileItem in FileItems)
            {
                fileItem.Assemble(labels);
            }
        }

        public void Save(FileInfo output)
        {
            var fileStream = output.OpenWrite();

            Save(fileStream);

            fileStream.Close();
        }

        public void Save(Stream stream)
        {

            foreach (var item in FileItems)
            {
                var bin = item.Bin;
                stream.Write(bin, 0, bin.Length);
            }
        }
    }
}
