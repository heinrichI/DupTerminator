using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class ArchiveFileInfo : ExtendedFileInfo
    {
        public uint ArchiveCRC { get; set; }

        public string ArchivePath { get; set; }

        public string ArchiveExtension { get; set; }

        public string ArchiveFileName { get; set; }
        public bool ArchiveInArchive { get; set; }

        //[JsonIgnore]
        //public override string CombinedPath => $"{ArchivePath}\\{ArchiveFileName}";

        //public override string Path { get; set; } => $"{ArchivePath}\\{ArchiveFileName}";


        public override bool Equals(object? obj)
        {
            return obj is ArchiveFileInfo info &&
                   CheckSum == info.CheckSum &&
                   Size == info.Size &&
                   Name == info.Name &&
                   Path == info.Path &&
                   LastAccessTime == info.LastAccessTime &&
                   LastWriteTime == info.LastWriteTime &&
                   DirectoryName == info.DirectoryName &&
                   Extension == info.Extension &&
                   //InArchive == info.InArchive &&
                   ArchiveCRC == info.ArchiveCRC &&
                   ArchivePath == info.ArchivePath &&
                   EqualityComparer<ExtendedFileInfo>.Default.Equals(Container, info.Container) &&
                   ArchiveExtension == info.ArchiveExtension &&
                   ArchiveFileName == info.ArchiveFileName;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(CheckSum);
            hash.Add(Size);
            hash.Add(Name);
            hash.Add(Path);
            hash.Add(LastAccessTime);
            hash.Add(LastWriteTime);
            hash.Add(DirectoryName);
            hash.Add(Extension);
            //hash.Add(InArchive);
            hash.Add(ArchiveCRC);
            hash.Add(ArchivePath);
            hash.Add(Container);
            hash.Add(ArchiveExtension);
            hash.Add(ArchiveFileName);
            return hash.ToHashCode();
        }
    }
}
