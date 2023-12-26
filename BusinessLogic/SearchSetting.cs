using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public class SearchSetting
    {
        public List<string> IncludePattern { get; set; } = new List<string>();
        public List<string> ExcludePattern { get; set; } = new List<string>();
        public bool UseDB { get; set; }
    }
}
