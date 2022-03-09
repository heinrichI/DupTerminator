using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic
{
    public class ProgressWithTimer<T> : IProgress<T> where T : class, IEquatable<T>
    {
        private T _previousProgressInfo;
        private volatile T _progressInfo;
        private readonly Action<T> _updateProgressAction;
        private readonly Timer _timer;
        private readonly SynchronizationContext _synchronizationContext;

        public ProgressWithTimer(TimeSpan pollingInterval, Action<T> updateProgressAction)
        {
            _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
            _updateProgressAction = updateProgressAction;
            _timer = new Timer(TimerCallback, null, pollingInterval, pollingInterval);
        }

        private void TimerCallback(object state)
        {
            ProcessUpdate();
        }

        private void ProcessUpdate()
        {
            var progressInfo = _progressInfo;
            if (_previousProgressInfo != progressInfo)
            {
                _synchronizationContext.Send(state => _updateProgressAction((T)state), progressInfo);
            }
            _previousProgressInfo = progressInfo;
        }

        public void Report(T value)
        {
            _progressInfo = value;
            //if (value.IsCompleted)
            //{
            //    _timer.Dispose();
             //   ProcessUpdate();
            //}
        }
    }
}
