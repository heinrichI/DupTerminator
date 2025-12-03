using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class PHashFileInfo
    {
        public PHashFileInfo(ExtendedFileInfo fileInfo)
        {
            FileInfo = fileInfo;
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

        public int Width { get; internal set; }
        public int Height { get; internal set; }
        public ExtendedFileInfo FileInfo { get; }
    }
}
