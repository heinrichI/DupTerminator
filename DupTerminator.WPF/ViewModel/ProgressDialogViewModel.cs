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
using System.Windows.Threading;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Commands;
using Microsoft.Extensions.Logging;

namespace DupTerminator.WPF.ViewModel
{
    public sealed class ProgressDialogViewModel : PropertyChangedBase,
                                               IProgressDialogVm,
                                               IDisposable
    {
        private readonly CancellationTokenSource _cts = new();
        public CancellationToken Token => _cts.Token;

        public ObservableCollection<ProgressItemViewModel> ProgressItems { get; } = new();

        //private readonly Dispatcher _dispatcher;

        public IProgress<ProgressDto> Progress { get; }

        public ProgressDialogViewModel()
        {
            //_dispatcher = Dispatcher.CurrentDispatcher;
            Progress = new Progress<ProgressDto>(UpdateProgress);
        }

        public void UpdateProgress(ProgressDto dto)
        {
            //_dispatcher.BeginInvoke(() => // Ensure UI thread
            //{
                //System.Diagnostics.Debug.WriteLine($"{dto.PhisicalDrive} {dto.State} {dto.Status}");
                var item = ProgressItems.SingleOrDefault(i => i.PhisicalDrive == dto.PhisicalDrive);
                if (item != null)  // update
                {
                    item.Status = dto.Status;
                    item.State = dto.State;
                    item.RemainSize = dto.RemainSize;
                }
                else   // insert
                {
                    item = new ProgressItemViewModel(dto);
                    ProgressItems.Add(item);
                }
            //}, DispatcherPriority.Background);
        }

        private void UpdateCore(ProgressDto dto)
        {
            System.Diagnostics.Debug.WriteLine($"{dto.PhisicalDrive} {dto.State} {dto.Status}");
            var item = ProgressItems.SingleOrDefault(i => i.PhisicalDrive == dto.PhisicalDrive);
            if (item != null)  // update
            {
                item.Status = dto.Status;
                item.State = dto.State;
                item.RemainSize = dto.RemainSize;
            }
            else   // insert
            {
                item = new ProgressItemViewModel(dto);
                ProgressItems.Add(item);
            }
        }

        public void Dispose() => _cts.Cancel();   // automatic on close

        ICommand _cancelCommand;

        //        /// <summary>
        //        /// The Cancel command.
        //        /// </summary>
        public ICommand CancelCommand
        {
            get
            {
                return _cancelCommand ?? (_cancelCommand = new RelayCommand(arg =>
                {

                    //    // Cancel all pending background tasks
                    _cts.Cancel();
                    }, arg => true));
            }
        }
    }
}
