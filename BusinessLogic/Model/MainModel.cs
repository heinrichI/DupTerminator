using DupTerminator.BusinessLogic.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DupTerminator.BusinessLogic.Model
{
    public class MainModel
    {
        public MainModel()
        {
            PathOfSearch = new List<DuplicateDirectory>();
            PathOfSkip = new List<DuplicateDirectory>();
        }

        public List<DuplicateDirectory> PathOfSearch { get; set; }
        public List<DuplicateDirectory> PathOfSkip { get; set; }
    }
}
