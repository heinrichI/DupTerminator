using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    [DebuggerDisplay("{FileItem.Name}")]
    public class PHashFileInfoSearchItem
    {
        public PHashFileInfoSearchItem(PHashFileInfo fileItem, SearchType type)
        {
            FileItem = fileItem;
            Type = type;
        }

        public PHashFileInfoSearchItem(PHashFileInfo fileItem, int hammingDistance)
        {
            FileItem = fileItem;
            HammingDistance = hammingDistance;
            Type = SearchType.Query;
        }

        public PHashFileInfo FileItem { get; }
        public SearchType Type { get; }
        public int HammingDistance { get; }

        public enum SearchType
        {
            Seed,
            Query
        }
    }
}
