using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class DuplicateGroup
    {
        public DuplicateGroup(string checksum, IList<ExtendedFileInfo> files)
        {
            Files = files;
            Checksum = checksum;
            if (files.All(f => f.Name == files[0].Name))
                Name = files[0].Name;
            else
                Name = checksum;
        }

        public IList<DuplicateContainer> Containers { get; set; }

        public string Name { get; set; }
        public string Checksum { get; internal set; }
        public IEnumerable<ExtendedFileInfo> Files { get; internal set; }

        public override bool Equals(object? obj)
        {
            return obj is DuplicateGroup group &&
                   Name == group.Name &&
                   Checksum == group.Checksum &&
                   EqualityComparer<IEnumerable<ExtendedFileInfo>>.Default.Equals(Files, group.Files);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Containers, Name, Checksum, Files);
        }
    }
}
