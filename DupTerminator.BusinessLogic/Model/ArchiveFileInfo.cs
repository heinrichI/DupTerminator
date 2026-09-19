using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class ArchiveFileInfo : ExtendedFileInfo, IArchiveItem
    {
        /// <summary>
        /// Контрольная сумма файла в архиве.
        /// </summary>
        public uint ArchiveCRC { get; set; }

        private string _archivePath;
        /// <summary>
        /// Путь к архиву.
        /// </summary>
        public string ArchivePath
        {
            get => _archivePath;
            set => _archivePath = string.Intern(value);
        }

        private string _archiveExtension;
        /// <summary>
        /// Расширение архива.
        /// </summary>
        public string ArchiveExtension
        {
            get => _archiveExtension;
            set => _archiveExtension = string.Intern(value);
        }

        private string _archiveFileName;
        /// <summary>
        /// Имя архива.
        /// </summary>
        public string ArchiveFileName
        {
            get => _archiveFileName;
            set => _archiveFileName = string.Intern(value);
        }

        public virtual bool ArchiveInArchive { get; set; }

        public DateTime GetContainerLastWriteTime()
        {
            if (ArchiveInArchive)
                return ((ExtendedFileInfo)((ExtendedFileInfo)Container).Container).LastWriteTime;
            return ((ExtendedFileInfo)Container).LastWriteTime;
        }



        //[JsonIgnore]
        //public override string CombinedPath => $"{ArchivePath}\\{ArchiveFileName}";

        //public override string Path { get; set; } => $"{ArchivePath}\\{ArchiveFileName}";


        public override bool Equals(object? obj)
        {
            return obj is ArchiveFileInfo info &&
                   Size == info.Size &&
                   Name == info.Name &&
                   Path == info.Path &&
                   //LastAccessTime == info.LastAccessTime &&
                   LastWriteTime == info.LastWriteTime &&
                   DirectoryName == info.DirectoryName &&
                   Extension == info.Extension &&
                   //InArchive == info.InArchive &&
                   ArchiveCRC == info.ArchiveCRC &&
                   ArchivePath == info.ArchivePath &&
                   //EqualityComparer<ExtendedFileInfo>.Default.Equals(Container, info.Container) &&
                   ArchiveExtension == info.ArchiveExtension &&
                   ArchiveFileName == info.ArchiveFileName;
        }

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
            hash.Add(ArchiveCRC);
            hash.Add(ArchivePath);
            hash.Add(Container);
            hash.Add(ArchiveExtension);
            hash.Add(ArchiveFileName);
            return hash.ToHashCode();
        }
    }
}
