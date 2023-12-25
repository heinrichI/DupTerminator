using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DupTerminator.BusinessLogic;
using System.Diagnostics;

namespace DupTerminator.BusinessLogic
{
    /// <summary>
    /// Extension of system.IO.FileInfo to contain the file checksum as well. 
    /// FileInfo can not be inherited since it is sealed.
    /// </summary>
    [DebuggerDisplay("{CheckSum} {Name}")]
    public class ExtendedFileInfo
    {
        //public byte[] Chunk;

        public string CheckSum { get; set; }

        public ulong Size { get; set; }

        public string Name { get; set; }

        public string? Path { get; set; }

        public DateTime LastAccessTime { get; set; }

        public DateTime LastWriteTime { get; set; }

        public string? DirectoryName { get; set; }
        public string Extension { get; set; }
        public bool InArchive { get; set; }
        public uint ArchiveCRC { get; set; }
        public string ArchivePath { get; set; }
        public ExtendedFileInfo Container { get; set; }

        public string CombinedPath => Path + ArchivePath;
    }
}
