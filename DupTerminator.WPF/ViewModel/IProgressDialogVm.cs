using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.WPF.ViewModel
{
    public interface IProgressDialogVm : INotifyPropertyChanged
    {
        ObservableCollection<ProgressItemViewModel> ProgressItems { get; }

        void UpdateProgress(ProgressDto dto);           // data path
        CancellationToken Token { get; }        // cancel path
    }
}
