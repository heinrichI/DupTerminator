using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public struct ContainerInfo
    {
        public string CheckSum { get; set; }

        public ulong Size { get; set; }

        public string Name { get; set; }

        public string? Path { get; set; }

        public DateTime LastAccessTime { get; set; }

        public DateTime LastWriteTime { get; set; }

        public string? DirectoryName { get; set; }
        public string Extension { get; set; }
        public bool InArchive { get; set; }
    }
}
