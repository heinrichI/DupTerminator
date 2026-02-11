using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;

namespace DupTerminator.DataBase.Dto
{
    [MemoryPackable]
    public partial class ArchiveSimpleFileInfoDto
    {
        public ulong Size { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public string? ArchivePath { get; set; }
    }
}
