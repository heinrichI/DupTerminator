using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class DuplicateContainer
    {
        public DuplicateContainer(KeyValuePair<(ContainerEqInfo, ContainerEqInfo), (List<ExtendedFileInfo>, List<ExtendedFileInfo>)> c)
        {
            FirstInfo = c.Key.Item1;
            SecondInfo = c.Key.Item2;
            FirstContainerFilesCount = c.Key.Item1.ContainerFilesCount;
            SecondContainerFilesCount = c.Key.Item2.ContainerFilesCount;
            FirstEqualFiles = c.Value.Item1;
            SecondEqualFiles = c.Value.Item2;
        }

        public DuplicateContainer((ContainerEqInfo, ContainerEqInfo) key, List<ExtendedFileInfo> firstFiles, List<ExtendedFileInfo> secondFiles, bool theyThemselvesAreEqual)
        {
            FirstInfo = key.Item1;
            SecondInfo = key.Item2;
            FirstContainerFilesCount = key.Item1.ContainerFilesCount;
            SecondContainerFilesCount = key.Item2.ContainerFilesCount;
            FirstEqualFiles = firstFiles;
            SecondEqualFiles = secondFiles;
            TheyThemselvesAreEqual = theyThemselvesAreEqual;


        }

        public List<ExtendedFileInfo> FirstEqualFiles { get; }
        public List<ExtendedFileInfo> SecondEqualFiles { get; }
        public bool TheyThemselvesAreEqual { get; }

        public int FirstEqualCount => FirstEqualFiles.Count;
        public int SeconEqualCount => SecondEqualFiles.Count;

        public ContainerEqInfo FirstInfo { get; }

        public ContainerEqInfo SecondInfo { get; }

        public int FirstContainerFilesCount { get; }

        public int SecondContainerFilesCount { get;  }

        public decimal SizeOfEqualFiles => TheyThemselvesAreEqual ? FirstInfo.FileInfo.Size : FirstEqualFiles.Distinct().Sum(f => (decimal)f.Size);

        public SimpleFileInfo[] FirstDiffrentFiles { get; set; }

        public SimpleFileInfo[] SecondDiffrentFiles { get; set; }

        //public string Similarity { get; set; }
    }
}
