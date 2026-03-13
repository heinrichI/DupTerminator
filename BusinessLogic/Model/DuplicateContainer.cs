using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class DuplicateContainer
    {
        public DuplicateContainer(ContainerPairKey key, SortedSet<ExtendedFileInfo> firstEqualFiles, SortedSet<ExtendedFileInfo> secondEqualFiles, bool theyThemselvesAreEqual)
        {
            Key = key;
            First = key.First;
            Second = key.Second;
            FirstContainerFilesCount = Key.FirstContainerFiles.Length;
            SecondContainerFilesCount = Key.SecondContainerFiles.Length;
            FirstEqualFiles = firstEqualFiles;
            SecondEqualFiles = secondEqualFiles;
            TheyThemselvesAreEqual = theyThemselvesAreEqual;
            //Debug.Assert(First.ContainerFilesCount != 0);
            //Debug.Assert(Second.ContainerFilesCount != 0);
        }


        public DuplicateContainer(KeyValuePair<ContainerPairKey, (SortedSet<ExtendedFileInfo>, SortedSet<ExtendedFileInfo>)> c)
        {
            Key = c.Key;
            First = c.Key.First;
            Second = c.Key.Second;
            FirstContainerFilesCount = Key.FirstContainerFiles.Length;
            SecondContainerFilesCount = Key.SecondContainerFiles.Length;
            //FirstContainerFilesCount = Key.FirstContainerFiles?.Length ?? c.Value.Item1.First().ContainerFilesCount;
            //SecondContainerFilesCount = Key.SecondContainerFiles?.Length ?? c.Value.Item2.First().ContainerFilesCount;
            FirstEqualFiles = c.Value.Item1;
            SecondEqualFiles = c.Value.Item2;
            Debug.Assert(FirstContainerFilesCount > 0);
            Debug.Assert(SecondContainerFilesCount > 0);
        }

        public DuplicateContainer(
            ExtendedFileInfo firstContainer,
            ExtendedFileInfo secondContainer,
            SortedSet<ExtendedFileInfo> firstEqualFiles,
            SortedSet<ExtendedFileInfo> secondEqualFiles)
        {
            First = firstContainer;
            Second = secondContainer;
            //FirstContainerFilesCount = firstContainer.ContainerFilesCount;
            //SecondContainerFilesCount = secondContainer.ContainerFilesCount;
            FirstEqualFiles = firstEqualFiles;
            SecondEqualFiles = secondEqualFiles;
        }

        public SortedSet<ExtendedFileInfo> FirstEqualFiles { get; set; }
        public SortedSet<ExtendedFileInfo> SecondEqualFiles { get; set; }
        public ContainerPairKey Key { get; }
        public ExtendedFileInfo First { get; }
        public ExtendedFileInfo Second { get; }
        public bool TheyThemselvesAreEqual { get; }

        public int FirstEqualCount => FirstEqualFiles.Count;
        public int SecondEqualCount => SecondEqualFiles.Count;

        public int FirstContainerFilesCount { get; }

        public int SecondContainerFilesCount { get; }

        public decimal SizeOfEqualFiles => TheyThemselvesAreEqual ? Key.First.Size : Math.Min(FirstEqualFiles.Distinct().Sum(f => (decimal)f.Size), SecondEqualFiles.Distinct().Sum(f => (decimal)f.Size));

        public SimpleFileInfo[] FirstDiffrentFiles { get; set; }

        public SimpleFileInfo[] SecondDiffrentFiles { get; set; }


        //public string Similarity { get; set; }
    }
}
