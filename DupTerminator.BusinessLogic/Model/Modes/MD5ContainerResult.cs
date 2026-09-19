using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model.Modes
{
    public class MD5ContainerResult : ResultBase
    {
        public MD5ContainerResult(Collection<DuplicateContainer> result)
        {
            if (result is not null)
                Result = new ObservableCollection<DuplicateContainer>(result);
        }
        public ObservableCollection<DuplicateContainer> Result { get; }
    }
}
