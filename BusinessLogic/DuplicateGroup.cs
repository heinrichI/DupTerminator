using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public class DuplicateGroup
    {
        public DuplicateGroup(KeyValuePair<string, IList<ExtendedFileInfo>> pair)
        {
            Files = pair.Value;
            Checksum = pair.Key;
        }

        public DuplicateGroup(IGrouping<string, ExtendedFileInfo> f)
        {
            Checksum = f.Key;
            Files = (IList<ExtendedFileInfo>?)f;
        }

        public IList<ExtendedFileInfo> Files { get; }

        public string Checksum { get; }
    }
}
