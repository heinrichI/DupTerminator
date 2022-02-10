using DupTerminator.BusinessLogic.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.View
{
    enum SeacrhState
    {
        Search,
        Stop
    }

    internal class MainViewModel
    {
        public MainViewModel()
        {
            PathOfSearch = new List<DuplicateDirectory>();
            PathOfSkip = new List<DuplicateDirectory>();
        }

        public List<DuplicateDirectory> PathOfSearch { get; set; }
        public List<DuplicateDirectory> PathOfSkip { get; set; }

        public SeacrhState State { get; set; }
}
}
