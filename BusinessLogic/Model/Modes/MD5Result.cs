using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic.Model.Modes
{
    public class MD5Result : ResultBase
    {
        public MD5Result(ReadOnlyCollection<DuplicateGroup> result)
        {
            Result = result;
        }

        public ReadOnlyCollection<DuplicateGroup> Result { get; }
    }
}
