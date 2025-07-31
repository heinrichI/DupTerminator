using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.WPF.ViewModel
{
    public class SearchCompletedEventArgs : EventArgs
    {
        public ReadOnlyCollection<DuplicateGroup> Results { get; }

        public SearchCompletedEventArgs(ReadOnlyCollection<DuplicateGroup> results)
        {
            Results = results;
        }
    }
}
