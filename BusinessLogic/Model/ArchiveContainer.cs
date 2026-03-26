using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class ArchiveContainer :  ContainerInfo
    {
        public ArchiveContainer()
        {            
        }

        public ArchiveContainer(ExtendedFileInfo efi)
        {
            Path = efi.Path;
            DirectoryName = efi.DirectoryName;
            Name = efi.Name;
            Size = efi.Size;
            Extension = efi.Extension;
            LastWriteTime = efi.LastWriteTime;
            Container = efi.Container;
            Debug.Assert(!string.IsNullOrEmpty(Path));
        }
    }
}
