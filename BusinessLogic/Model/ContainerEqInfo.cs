using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class ContainerEqInfo
    {
        public ContainerEqInfo(string path)
        {
            Path = path;
        }

        public ContainerEqInfo(string path, int containerFilesCount) : this(path)
        {
            ContainerFilesCount = containerFilesCount;
        }

        public string Path { get; }
        public int ContainerFilesCount { get; }

        public override bool Equals(object? obj)
        {
            return obj is ContainerEqInfo info &&
                   Path == info.Path;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Path);
        }
    }
}
