using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic.Model.Modes
{
    public class PHashResult : ResultBase
    {
        public PHashResult()
        {

        }

        public PHashResult(ReadOnlyCollection<PHashDuplicateGroup> result)
        {
            Result = result;
        }

        public ReadOnlyCollection<PHashDuplicateGroup> Result { get; }
    }
}
