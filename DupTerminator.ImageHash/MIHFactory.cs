using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Abstraction;

namespace DupTerminator.ImageHash
{
    internal class MIHFactory : IMIHFactory
    {
        public IMIH Create()
        {
            return new MIHMy();
        }
    }
}
