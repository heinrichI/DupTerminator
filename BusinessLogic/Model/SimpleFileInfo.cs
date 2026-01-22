using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    [DebuggerDisplay("{Name}")]
    public class SimpleFileInfo
    {
        public SimpleFileInfo()
        {            
        }

        public SimpleFileInfo(ExtendedFileInfo c)
        {
            Size = c.Size;
            Name = c.Name;
            Path = c.Path;
        }

        public ulong Size { get; set; }

        private string? _name;
        public string? Name
        {
            get => _name;
            set => _name = value is null ? null : string.Intern(value);
        }

        private string _path;
        public string Path
        {
            get => _path;
            set => _path = string.Intern(value);
        }

        public override bool Equals(object? obj)
        {
            return obj is SimpleFileInfo info && Equals(info);
        }

        public bool Equals(SimpleFileInfo info)
        {
            return Size == info.Size &&
                   Name == info.Name &&
                   Path == info.Path;
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Size);
            hash.Add(Name);
            hash.Add(Path);
            return hash.ToHashCode();
        }

    }
}
