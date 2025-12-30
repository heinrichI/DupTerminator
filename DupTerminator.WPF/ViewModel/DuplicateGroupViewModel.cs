using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.WPF.ViewModel
{
    public class DuplicateGroupViewModel
    {
        public List<ExtendedFileInfoViewModel> Files { get; internal set; } = new List<ExtendedFileInfoViewModel>();
    }
}
