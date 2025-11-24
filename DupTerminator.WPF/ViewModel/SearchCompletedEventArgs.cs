using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.WPF.Model;

namespace DupTerminator.WPF.ViewModel
{
    public class SearchCompletedEventArgs : EventArgs
    {
        //public ReadOnlyCollection<ResultBase> Results { get; }
        public ResultBase Results { get; }

        //public SearchCompletedEventArgs(ReadOnlyCollection<ResultBase> results)
        public SearchCompletedEventArgs(ResultBase results)
        {
            Results = results;
        }
    }
}
