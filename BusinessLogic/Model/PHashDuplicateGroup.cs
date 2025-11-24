using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class PHashDuplicateGroup : HashSet<PHashFileInfoSearchItem>
    {
        internal bool ContainsPath(string path)
        {
            return this.Any(f => f.FileItem.Path == path);
        }
    }
}
