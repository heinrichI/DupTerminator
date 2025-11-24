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
            FirstPath = c.Key.Item1.Path;
            SecondPath = c.Key.Item2.Path;
            FirstContainerFilesCount = c.Key.Item1.ContainerFilesCount;
            SecondContainerFilesCount = c.Key.Item2.ContainerFilesCount;
            FirstEqualFiles = c.Value.Item1;
            SecondEqualFiles = c.Value.Item2;
        }

        public IList<ExtendedFileInfo> FirstEqualFiles { get; }
        public IList<ExtendedFileInfo> SecondEqualFiles { get; }

        public int FirstEqualCount => FirstEqualFiles.Count;
        public int SeconEqualCount => SecondEqualFiles.Count;

        public string FirstPath { get; }
        public string SecondPath { get; }
        public int FirstContainerFilesCount { get; private set; }
        public int SecondContainerFilesCount { get; private set; }

        //public string Similarity { get; set; }
    }
}
