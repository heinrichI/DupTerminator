using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DupTerminator.ObjectModel;

namespace DupTerminator.Models
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
