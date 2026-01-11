using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;

namespace DupTerminator.BusinessLogic.Model
{
    /// <summary>
    /// Extension of system.IO.FileInfo to contain the file checksum as well. 
    /// FileInfo can not be inherited since it is sealed.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    public class ExtendedFileInfo : SimpleFileInfo
    {
        //public byte[] Chunk;


        //public DateTime LastAccessTime { get; set; }

        public DateTime LastWriteTime { get; set; }

        public string? DirectoryName { get; set; }
        public string Extension { get; set; }

        //public bool InArchive { get; set; }

        public int ContainerFilesCount { get; set; }

        public ExtendedFileInfo Container { get; set; }

        //[JsonIgnore]
        //public virtual string CombinedPath => Path;



        public override bool Equals(object? obj)
        {
            return obj is ExtendedFileInfo info && Equals(info);
        }

        public bool Equals(ExtendedFileInfo info)
        {
            return Size == info.Size &&
                   Name == info.Name &&
                   Path == info.Path &&
                   //LastAccessTime == info.LastAccessTime &&
                   LastWriteTime == info.LastWriteTime &&
                   DirectoryName == info.DirectoryName &&
                   Extension == info.Extension;
                   //EqualityComparer<ExtendedFileInfo>.Default.Equals(Container, info.Container);
        }

        //public override bool Equals(object? obj)
        //{
        //    return obj is ExtendedFileInfo info &&
        //           CheckSum == info.CheckSum &&
        //           Size == info.Size &&
        //           Name == info.Name &&
        //           Path == info.Path &&
        //           LastAccessTime == info.LastAccessTime &&
        //           LastWriteTime == info.LastWriteTime &&
        //           DirectoryName == info.DirectoryName &&
        //           Extension == info.Extension &&
        //           //InArchive == info.InArchive &&
        //           //ArchiveCRC == info.ArchiveCRC &&
        //           //ArchivePath == info.ArchivePath &&
        //           EqualityComparer<ExtendedFileInfo>.Default.Equals(Container, info.Container);
        //           //ArchiveExtension == info.ArchiveExtension &&
        //           //ArchiveFileName == info.ArchiveFileName;
        //}

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Size);
            hash.Add(Name);
            hash.Add(Path);
            //hash.Add(LastAccessTime);
            hash.Add(LastWriteTime);
            hash.Add(DirectoryName);
            hash.Add(Extension);
            //hash.Add(InArchive);
            //hash.Add(ArchiveCRC);
            //hash.Add(ArchivePath);
            //hash.Add(Container);
            //hash.Add(ArchiveExtension);
            //hash.Add(ArchiveFileName);
            return hash.ToHashCode();
        }
    }
}
