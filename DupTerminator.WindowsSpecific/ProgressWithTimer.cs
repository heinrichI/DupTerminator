using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.BusinessLogic.Service
{
    public class ProgressWithTimer<T> : IProgress<T> where T : class, IEquatable<T>
    //public class ProgressWithTimer<T> where T : class, IEquatable<T>
    {
        private readonly ConcurrentDictionary<string, T> _latestProgress = new();
        //private readonly Dictionary<string, T> _latestProgress = new(); // Track latest per drive
        //private readonly TimeSpan _interval;
        private readonly IProgress<T> _progress;
        //private readonly Action<T> _updateAction;
        //private readonly DispatcherTimer _timer;
        private PeriodicTimer _timer;
        private Task _reportingTask;

        //private readonly object _lock = new();
        private bool _hasPendingUpdate = false;
        private CancellationTokenSource _cts = new();

        public ProgressWithTimer(TimeSpan interval, IProgress<T> progress)
            //Action<T> updateAction)
        {
            //_interval = interval;
            _progress = progress;
            //_updateAction = updateAction;
            //_timer = new DispatcherTimer(DispatcherPriority.Background)
            //{
            //    Interval = interval
            //};
            //_timer.Tick += Timer_Tick;
            //_timer.Start();
            //StartTimer();
            _timer = new PeriodicTimer(interval);
            _reportingTask = ReportProgressAsync(_timer, _cts.Token);
        }

        private async Task ReportProgressAsync(
         PeriodicTimer timer,
         CancellationToken cancellationToken)
        {
            try
            {
                while (await timer.WaitForNextTickAsync(cancellationToken))
                {
                    if (_hasPendingUpdate)
                    {
                        foreach (var kvp in _latestProgress)
                            _progress.Report(kvp.Value);
                        //_updateAction(kvp.Value);
                        _hasPendingUpdate = false;
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when cancellation is requested
            }
        }

        //private async void StartTimer()
        //{
        //    while (_cts != null && !_cts.IsCancellationRequested)
        //    {
        //        await Task.Delay(_interval, _cts.Token).ContinueWith(tsk => tsk.Exception == default).ConfigureAwait(false);

        //        if (_hasPendingUpdate)
        //        {
        //            // Run UI update on dispatcher **only once** per interval
        //            //await _dispatcher.InvokeAsync(() =>
        //            //{
        //            //lock (_lock)
        //            {
        //                foreach (var kvp in _latestProgress)
        //                    _updateAction(kvp.Value);
        //                _hasPendingUpdate = false;
        //            }//).ConfigureAwait(false);
        //        }
        //    }
        //}

        //private void Timer_Tick(object sender, EventArgs e)
        //{
        //    if (_hasPendingUpdate)
        //    {
        //        //lock (_lock)
        //        {
        //            Debug.WriteLine("Call update UI");
        //            foreach (var kvp in _latestProgress)
        //            {
        //                _updateAction(kvp.Value); // Always update UI with latest per drive
        //            }
        //            // Optional: Clear after update? Not needed - we always want latest
        //            _hasPendingUpdate = false;
        //        }
        //    }
        //}

        public void Report(T value)
        {
            if (value is not ProgressDto dto)
                return;

            //lock (_lock)
            //{
                //Debug.WriteLine("Recive " + dto.Status);
                var key = dto.PhisicalDrive ?? string.Empty;
                _latestProgress[key] = value;// Always store latest
                _hasPendingUpdate = true;
            //}
        }

        //public void Dispose() => _timer.Stop();
        public void Dispose()
        {
            _cts.Cancel();
            //_reportingTask.Dispose();
            //_cts.Dispose();
        }
    }
}
