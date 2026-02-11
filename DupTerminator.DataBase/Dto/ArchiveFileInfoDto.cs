using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;
using MemoryPack;

namespace DupTerminator.DataBase.Dto
{
    [MemoryPackable]
    public partial class ArchiveFileInfoDto
    {
        public ulong Size { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public DateTime LastWriteTime { get; set; }
        public string? DirectoryName { get; set; }
        public string? Extension { get; set; }
        public int ContainerFilesCount { get; set; }
        public ExtendedFileInfoDto? Container { get; set; }

        public uint ArchiveCRC { get; set; }
        public string? ArchivePath { get; set; }
        public string? ArchiveExtension { get; set; }
        public string? ArchiveFileName { get; set; }
        public bool ArchiveInArchive { get; set; }
        public ArchiveSimpleFileInfoDto[]? ContainerFiles { get; set; }
    }
}
