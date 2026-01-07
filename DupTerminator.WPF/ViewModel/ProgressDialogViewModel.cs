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
using DupTerminator.WPF.Model;
using Microsoft.Extensions.Logging;

namespace DupTerminator.WPF.ViewModel
{
    public sealed class ProgressDialogViewModel : PropertyChangedBase,
                                               IProgressDialogVm,
                                               IDisposable
    {
        private CancellationTokenSource _cts = new();
        public CancellationToken Token => _cts.Token;


        public ObservableCollection<ProgressItemViewModel> ProgressItems { get; } = new();

        public IProgress<ProgressDto> Progress { get; }

        private readonly Dispatcher _currentDispatcher;

        public ProgressDialogViewModel()
        {
            Progress = new Progress<ProgressDto>(UpdateProgress);
            _currentDispatcher = Dispatcher.CurrentDispatcher;
        }

        public void UpdateProgress(ProgressDto dto)
        {
            //_dispatcher.BeginInvoke(() => // Ensure UI thread
            //{
                //System.Diagnostics.Debug.WriteLine($"{dto.PhisicalDrive} {dto.State} {dto.Status}");
                var item = ProgressItems.SingleOrDefault(i => i.PhisicalDrive == dto.PhisicalDrive);
                if (item != null)  // update
                {
                    item.Path = dto.Path;
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

        // Add a collection for logs
        public ObservableCollection<LogEntry> Logs { get; } = new();


        // Method to add log entries
        public void AddLogEntry(LogEntry entry)
        {
            _currentDispatcher.BeginInvoke(() =>
            {
                Logs.Add(entry);
            });
        }

        // Method to add log messages directly
        public void AddLogMessage(string message, string level = "INFO")
        {
            AddLogEntry(new LogEntry(DateTime.Now, level, message, "Application"));
        }

        internal void Clear()
        {
            //Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            //{
                ProgressItems.Clear();
                Logs.Clear();
            //});


            // 1. Cancel and Dispose of the old source safely
            _cts.Cancel();
            _cts.Dispose();

            // 2. Create a fresh instance
            _cts = new CancellationTokenSource();
        }
    }
}
