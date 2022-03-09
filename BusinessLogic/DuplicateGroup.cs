using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public class DuplicateGroup
    {
        public DuplicateGroup(KeyValuePair<string, List<ExtendedFileInfo>> pair)
        {
            Files = pair.Value;
            Checksum = pair.Key;
        }

        public List<ExtendedFileInfo> Files { get; }

        public string Checksum { get; }
    }
}
