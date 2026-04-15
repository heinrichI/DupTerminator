using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json.Serialization;

[assembly: DebuggerDisplay(
    "{Key,nq}  {((System.Collections.Generic.IList<DupTerminator.BusinessLogic.Model.ExtendedFileInfo>)Value)[0].Name,nq}",
    Target = typeof(KeyValuePair<,>))]
namespace DupTerminator.BusinessLogic.Model
{
    /// <summary>
    /// Extension of system.IO.FileInfo to contain the file checksum as well. 
    /// FileInfo can not be inherited since it is sealed.
    /// </summary>
    [DebuggerDisplay("{Name}")]
    [JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
    [JsonDerivedType(typeof(ArchiveFileInfo), typeDiscriminator: "archive")]
    [JsonDerivedType(typeof(ArchiveContainer), typeDiscriminator: "archiveContainer")]
    [JsonDerivedType(typeof(PdfFileInfo), typeDiscriminator: "pdf")]
    [JsonDerivedType(typeof(PdfContainer), typeDiscriminator: "pdfContainer")]
    [JsonDerivedType(typeof(DirectoryContainer), typeDiscriminator: "directoryContainer")]
    public class ExtendedFileInfo : SimpleFileInfo, IComparable
    {
        public DateTime LastWriteTime { get; set; }

        private string? _directoryName;
        public string? DirectoryName
        {
            get => _directoryName;
            set => _directoryName = value is null ? null : string.Intern(value);
        }

        private string? _extension;

        public ExtendedFileInfo()
        {
        }

        //public ExtendedFileInfo(SimpleFileInfo sfi)
        //{
        //    Name = sfi.Name;
        //    Path = sfi.Path;
        //    Size = sfi.Size;
        //}

        public string? Extension
        {
            get => _extension;
            set => _extension = value is null ? null : string.Intern(value);
        }

        public ContainerInfo Container { get; set; }

        //public IEnumerable<ExtendedFileInfo> GetAllParentsContainers()
        //{
        //    var current = Container;
        //    while (current != null && !string.IsNullOrEmpty(current.Name))
        //    {
        //        yield return current;
        //        current = current.Container;
        //    }
        //}


        public IEnumerable<ExtendedFileInfo> GetAncestorContainers()
        {
            var current = this.Container;
            while (current != null)
            {
                yield return current;
                current = current.Container;
            }
        }

        public override bool Equals(object? obj)
        {
            return obj is ExtendedFileInfo info && Equals(info);
        }

        public bool Equals(ExtendedFileInfo info)
        {
            return Size == info.Size &&
                   Name == info.Name &&
                   Path == info.Path &&
                   LastWriteTime == info.LastWriteTime &&
                   DirectoryName == info.DirectoryName &&
                   Extension == info.Extension;
                   //EqualityComparer<ExtendedFileInfo>.Default.Equals(Container, info.Container);
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Size);
            hash.Add(Name);
            hash.Add(Path);
            hash.Add(LastWriteTime);
            hash.Add(DirectoryName);
            hash.Add(Extension);
            return hash.ToHashCode();
        }

        public int Compare(ExtendedFileInfo x, ExtendedFileInfo y)
        {
            int result = string.Compare(x.Name, y.Name);
            // If names are the same, compare paths so both files are kept
            return result != 0 ? result : string.Compare(x.Path, y.Path);
        }

        // Generic implementation (Fastest)
        public int CompareTo(ExtendedFileInfo? other)
        {
            if (other is null) return 1; // Current instance follows null

            // 1. Sort by Name (Case-insensitive)
            int result = string.Compare(this.Name, other.Name, StringComparison.OrdinalIgnoreCase);

            // 2. Tie-breaker: If names are identical, compare FullPath
            // This prevents different files with the same name from being excluded from the Set.
            if (result == 0)
            {
                result = string.Compare(this.Path, other.Path, StringComparison.OrdinalIgnoreCase);
            }

            return result;
        }

        // Non-generic implementation (Required to fix your specific error)
        public int CompareTo(object? obj)
        {
            if (obj is null) return 1;
            if (obj is ExtendedFileInfo other) return CompareTo(other);

            throw new ArgumentException("Object must be of type SimpleFileInfo");
        }

        public override string ToString()
        {
            return $"{this.GetType().Name}: {Name}";
        }
    }
}
