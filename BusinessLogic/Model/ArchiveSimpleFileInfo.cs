using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class ArchiveSimpleFileInfo : SimpleFileInfo
    {
        public ArchiveSimpleFileInfo()
        {
        }
        public ArchiveSimpleFileInfo(ArchiveFileInfo c) : base(c)
        {
            ArchivePath = c.ArchivePath;
        }

        public string ArchivePath { get; set; }
    }
}
