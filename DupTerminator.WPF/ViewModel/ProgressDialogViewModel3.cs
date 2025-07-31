using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DupTerminator.BusinessLogic;
using DupTerminator.WPF.Commands;

namespace DupTerminator.WPF.ViewModel
{
    //public sealed class ProgressDialogViewModel3 : PropertyChangedBase
    //{
    //    private readonly CancellationTokenSource _cancellationTokenSource = new();

    //    public ObservableCollection<ProgressItemViewModel> ProgressItems { get; }
    //        = new ObservableCollection<ProgressItemViewModel>();

    //    public ICommand CancelCommand { get; }
    //    public CancellationToken CancellationToken => _cancellationTokenSource.Token;

    //    public ProgressDialogViewModel()
    //    {
    //        CancelCommand = new RelayCommand(_ => Cancel());
    //    }

    //    public void UpdateProgress(ProgressDto dto)
    //    {
    //        string driveKey = dto.PhisicalDrive ?? "Unknown";

    //        // Update on UI thread
    //        Application.Current.Dispatcher.Invoke(() =>
    //        {
    //            var existingItem = ProgressItems.FirstOrDefault(item => item.PhisicalDrive == driveKey);
    //            if (existingItem != null)
    //            {
    //                existingItem.Status = dto.Status;
    //            }
    //            else
    //            {
    //                ProgressItems.Add(new ProgressItemViewModel
    //                {
    //                    PhisicalDrive = driveKey,
    //                    Status = dto.Status
    //                });
    //            }
    //        });
    //    }

    //    public void Cancel() => _cancellationTokenSource.Cancel();
    //}
}
