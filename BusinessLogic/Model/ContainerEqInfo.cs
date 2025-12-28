using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    [DebuggerDisplay("{Path}")]
    public class ContainerEqInfo
    {
        public ContainerEqInfo(ExtendedFileInfo fileInfo)
        {
            FileInfo = fileInfo;
        }

        public ContainerEqInfo(ExtendedFileInfo container, int containerFilesCount, ExtendedFileInfo fileInfo) : this(container)
        {
            ContainerFilesCount = containerFilesCount;
            if (fileInfo is ArchiveFileInfo afi)
                ContainerFiles = afi.ContainerFiles;
        }

        public string Path => FileInfo.Path;

        public int ContainerFilesCount { get; }

        public SimpleFileInfo[] ContainerFiles { get; }

        public ExtendedFileInfo FileInfo { get; }


        public override bool Equals(object? obj)
        {
            return obj is ContainerEqInfo info &&
                   Path == info.Path;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Path);
        }
    }
}
