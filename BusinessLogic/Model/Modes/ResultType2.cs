using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model.Modes
{
    public class ResultType2 : ResultBase
    {
        public double Discount { get; set; }
        public List<string> Tags { get; set; }
    }
}
