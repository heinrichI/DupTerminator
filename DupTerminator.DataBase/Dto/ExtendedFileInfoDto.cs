using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;

namespace DupTerminator.DataBase.Dto
{
    [MemoryPackable]
    public partial class ExtendedFileInfoDto
    {
        public ulong Size { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string? DirectoryName { get; set; }
        public string? Extension { get; set; }
        public int ContainerFilesCount { get; set; }
        public ExtendedFileInfoDto? Container { get; set; }
    }
}
