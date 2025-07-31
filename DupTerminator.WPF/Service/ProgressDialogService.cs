using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Service;
using DupTerminator.WPF.View;
using DupTerminator.WPF.ViewModel;

namespace DupTerminator.WPF.Service
{
    public sealed class ProgressDialogService : IProgressDialogService
    {
        public async Task RunAsync(Func<IProgress<ProgressDto>, CancellationToken, Task> worker)
        {
            var vm = new ProgressDialogViewModel();


            //// Create progress reporter with throttling
            var progress = new ProgressWithTimer<ProgressDto>(
                TimeSpan.FromMilliseconds(200),
                vm.Update
            );
            //IProgress<ProgressDto> progress = new ThrottledProgress<ProgressDto>(
            //    TimeSpan.FromMilliseconds(200),
            //    vm.Update
            //);

            var dialog = new ProgressWindow{
                Owner = Application.Current.MainWindow,
                DataContext = vm
            };


            // Create completion sources for coordination
            //var workerTaskSource = new TaskCompletionSource<bool>();
            //var dialogClosedSource = new TaskCompletionSource<bool>();

            // Handle dialog closing
            dialog.Closed += (_, _) =>
            {
                vm.Dispose(); // Cancel the token
                progress?.Dispose();
                //dialogClosedSource.TrySetResult(true);
            };

            // Register cancellation callback
            //vm.Token.Register(() =>
            //{
            //    if (dialog.Dispatcher.CheckAccess())
            //        dialog.Close();
            //    else
            //        dialog.Dispatcher.Invoke(dialog.Close);
            //});

            try
            {
                // Start the worker operation
                var workerTask = worker(progress, vm.Token);
                //workerTaskSource.SetResult(true);
                _ = workerTask.ContinueWith(t =>
                {
                    if (t.IsCompletedSuccessfully)
                    {
                        // Close dialog when worker completes
                        dialog.Dispatcher.Invoke(dialog.Close);
                    }
                }, TaskScheduler.Current);

                // Show dialog and wait for either worker completion or dialog close
                dialog.Show();

                // Wait for the worker task or dialog close
                //await Task.WhenAny(workerTask, dialogClosedSource.Task);
                await workerTask;
            }
            finally
            {
                // Ensure dialog is closed and resources are cleaned up
                if (dialog.IsLoaded)
                {
                    dialog.Dispatcher.Invoke(dialog.Close);
                }
                vm.Dispose();
                progress?.Dispose();
            }

            //// close the dialog in the view-model's dispose
            //dialog .Closed += (_, _) => vm.Dispose();

            //try
            //{
            //    vm.Token.Register(() =>
            //    {
            //        if (dialog .Dispatcher.CheckAccess())
            //            dialog .Close();
            //        else
            //            dialog .Dispatcher.Invoke(dlg.Close);
            //    });

            //    _ = worker(progress, vm.Token);   // fire & forget
            //    var result = dialog .ShowDialog();      // MVVM-toolkit extension
            //    //await Task.Run(() => dialog .ShowDialog());
            //}
            //finally
            //{
            //    vm.Dispose();
            //}
        }
    }
}
