using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class PHashFileInfo
    {
        public PHashFileInfo(ExtendedFileInfo fileInfo, ulong hash, int width, int height)
        {
            FileInfo = fileInfo;
            Hash = hash;
            Width = width;
            Height = height;
        }

        //public PHashFileInfo(ExtendedFileInfo data)
        //{
        //    Path = data.Path;
        //    Extension = data.Extension;
        //    CheckSum = data.CheckSum;
        //    Name = data.Name;
        //    Container = data.Container;
        //    ContainerFilesCount = data.ContainerFilesCount;
        //    LastAccessTime = data.LastAccessTime;
        //    LastWriteTime = data.LastWriteTime;
        //    DirectoryName = data.DirectoryName;
        //    Size = data.Size;
        //}

        public int Width { get; }
        public int Height { get; }
        public ExtendedFileInfo FileInfo { get; }
        public ulong Hash { get; }
    }
}
