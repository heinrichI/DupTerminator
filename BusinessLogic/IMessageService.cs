using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public interface IMessageService
    {
        void DeletedOutdatedRecord(uint deleted);
    }
}
