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
            FirstEqualFiles = c.Value.Item1;
            SecondEqualFiles = c.Value.Item2;
        }

        public DuplicateContainer((ContainerEqInfo, ContainerEqInfo) key, List<ExtendedFileInfo> firstFiles, List<ExtendedFileInfo> secondFiles, bool theyThemselvesAreEqual)
        {
            FirstInfo = key.Item1;
            SecondInfo = key.Item2;

            FirstEqualFiles = firstFiles;
            SecondEqualFiles = secondFiles;
            TheyThemselvesAreEqual = theyThemselvesAreEqual;
        }

        public DuplicateContainer(
            ExtendedFileInfo firstContainer,
            ExtendedFileInfo secondContainer,
            List<ExtendedFileInfo> firstEqualFiles,
            List<ExtendedFileInfo> secondEqualFiles)
        {
            FirstInfo = new ContainerEqInfo(firstContainer, firstContainer.ContainerFilesCount);
            SecondInfo = new ContainerEqInfo(secondContainer, secondContainer.ContainerFilesCount);
            FirstEqualFiles = firstEqualFiles;
            SecondEqualFiles = secondEqualFiles;
        }

        public List<ExtendedFileInfo> FirstEqualFiles { get; }
        public List<ExtendedFileInfo> SecondEqualFiles { get; }
        public bool TheyThemselvesAreEqual { get; }

        public int FirstEqualCount => FirstEqualFiles.Count;
        public int SeconEqualCount => SecondEqualFiles.Count;

        public ContainerEqInfo FirstInfo { get; }

        public ContainerEqInfo SecondInfo { get; }

        public int FirstContainerFilesCount => FirstInfo.ContainerFilesCount;

        public int SecondContainerFilesCount => SecondInfo.ContainerFilesCount;

        public decimal SizeOfEqualFiles => TheyThemselvesAreEqual ? FirstInfo.FileInfo.Size : Math.Min(FirstEqualFiles.Distinct().Sum(f => (decimal)f.Size), SecondEqualFiles.Distinct().Sum(f => (decimal)f.Size));

        public SimpleFileInfo[] FirstDiffrentFiles { get; set; }

        public SimpleFileInfo[] SecondDiffrentFiles { get; set; }

        //public string Similarity { get; set; }
    }
}
