using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model.Modes
{
    public class DuplicateContainerResult : ResultBase
    {
        public DuplicateContainerResult(ReadOnlyCollection<DuplicateContainer> result)
        {
            Result = result;
        }
        public ReadOnlyCollection<DuplicateContainer> Result { get; }
    }
}
