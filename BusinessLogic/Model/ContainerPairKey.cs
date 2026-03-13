using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static DupTerminator.BusinessLogic.SearcherMD5Container;

namespace DupTerminator.BusinessLogic.Model
{
    [DebuggerDisplay("{First.Name} {Second.Name}")]
    public class ContainerPairKey
    {
        public ContainerPairKey(ExtendedFileInfo first, ExtendedFileInfo second)
        {
            // Ensure the pair is ordered by path (to avoid duplicates)
            if (string.Compare(first.Path, second.Path, StringComparison.Ordinal) > 0)
            {
                // Swap if first.Path > second.Path
                First = second;
                Second = first;
                WasSwapped = true;

                if (first is DirectoryContainer dc)
                    SecondContainerFiles = dc.Files;
                else if(first is ArchiveContainer ac4)
                    SecondContainerFiles = ac4.Files;
                else if(first is PdfContainer pc3)
                    SecondContainerFiles = pc3.Files;
                else if (first.Container is DirectoryContainer dfi)
                    SecondContainerFiles = dfi.Files;
                else if(first.Container is ArchiveContainer ac)
                    SecondContainerFiles = ac.Files;
                else if (first.Container is PdfContainer pc2)
                    SecondContainerFiles = pc2.Files;

                if (second is ArchiveContainer sac)
                    FirstContainerFiles = sac.Files;
                else if (second is DirectoryContainer dfi3)
                    FirstContainerFiles = dfi3.Files;
                else if (second is PdfContainer pc3)
                    FirstContainerFiles = pc3.Files;
                else if(second.Container is ArchiveContainer ac2)
                    FirstContainerFiles = ac2.Files;
                else if (second.Container is DirectoryContainer dfi2)
                    FirstContainerFiles = dfi2.Files;
                else if (second.Container is PdfContainer pc)
                    FirstContainerFiles = pc.Files;
            }
            else
            {
                First = first;
                Second = second;

                if (first is ArchiveContainer ac4)
                    FirstContainerFiles = ac4.Files;
                else if(first is DirectoryContainer dfi)
                    FirstContainerFiles = dfi.Files;
                else if(first is PdfContainer pc3)
                    FirstContainerFiles = pc3.Files;
                else if(first.Container is ArchiveContainer ac)
                    FirstContainerFiles = ac.Files;
                else if (first.Container is DirectoryContainer dfi2)
                    FirstContainerFiles = dfi2.Files;
                else if (second.Container is PdfContainer pc)
                    FirstContainerFiles = pc.Files;

                if (second is ArchiveContainer ac3)
                    SecondContainerFiles = ac3.Files;
                else if(second is DirectoryContainer dfi3)
                    SecondContainerFiles = dfi3.Files;
                else if (second is PdfContainer pc3)
                    SecondContainerFiles = pc3.Files;
                else if(second.Container is ArchiveContainer ac2)
                    SecondContainerFiles = ac2.Files;
                else if (second.Container is DirectoryContainer dfi2)
                    SecondContainerFiles = dfi2.Files;
                else if (second.Container is PdfContainer pc)
                    SecondContainerFiles = pc.Files;
            }
            Debug.Assert(FirstContainerFiles is not null);
            Debug.Assert(SecondContainerFiles is not null);
        }

        public ContainerPairKey(ExtendedFileInfo first, ExtendedFileInfo second, ExtendedFileInfo firstChild, ExtendedFileInfo secondChild) : this(first, second)
        {
            // Ensure the pair is ordered by path (to avoid duplicates)
            if (string.Compare(first.Path, second.Path, StringComparison.Ordinal) > 0)
            {
                // Swap if first.Path > second.Path
                First = second;
                Second = first;
                WasSwapped = true;

                if (firstChild is ArchiveContainer afi)
                    SecondContainerFiles = afi.Files;
                else if (firstChild is DirectoryContainer dc)
                    SecondContainerFiles = dc.Files;
                else if (firstChild is PdfContainer pc)
                    SecondContainerFiles = pc.Files;
                if (secondChild is ArchiveContainer safi)
                    FirstContainerFiles = safi.Files;
                else if (secondChild is DirectoryContainer dc)
                    FirstContainerFiles = dc.Files;
                else if (secondChild is PdfContainer pc)
                    FirstContainerFiles = pc.Files;
            }
            else
            {
                First = first;
                Second = second;

                if (firstChild is ArchiveContainer ac)
                    FirstContainerFiles = ac.Files;
                else if (firstChild is DirectoryContainer dc)
                    FirstContainerFiles = dc.Files;
                else if (firstChild is PdfContainer pc)
                    FirstContainerFiles = pc.Files;
                if (secondChild is ArchiveContainer sac)
                    SecondContainerFiles = sac.Files;
                else if (secondChild is DirectoryContainer dc)
                    SecondContainerFiles = dc.Files;
                else if (secondChild is PdfContainer pc)
                    SecondContainerFiles = pc.Files;
            }
            Debug.Assert(FirstContainerFiles is not null);
            Debug.Assert(SecondContainerFiles is not null);
            Debug.Assert(FirstContainerFiles.Length > 0);
            Debug.Assert(SecondContainerFiles.Length > 0);
        }

        public ExtendedFileInfo First { get; }
        public ExtendedFileInfo Second { get; }
        public bool WasSwapped { get; }
        public SimpleFileInfo[] SecondContainerFiles { get; }
        public SimpleFileInfo[] FirstContainerFiles { get; }

        public bool Equals(ContainerPairKey other) =>
             First.GetHashCode() == other.First.GetHashCode() && Second.GetHashCode() == other.Second.GetHashCode();

        public override bool Equals(object? obj) =>
            obj is ContainerPairKey k && Equals(k);

        public override int GetHashCode() =>
            HashCode.Combine(First.GetHashCode(), Second.GetHashCode());
    }
}
