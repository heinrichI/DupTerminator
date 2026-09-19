using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Service
{
    public class ThrottledProgress<T> : IProgress<T>, IDisposable where T : class
    {
        private readonly Action<T> _updateProgressAction;
        private readonly TimeSpan _throttleInterval;
        private readonly SynchronizationContext _synchronizationContext;
        private DateTime _lastUpdate = DateTime.MinValue;
        private T _latestValue;
        private readonly object _lock = new object();
        private Timer _flushTimer;

        public ThrottledProgress(TimeSpan throttleInterval, Action<T> updateProgressAction)
        {
            _throttleInterval = throttleInterval;
            _synchronizationContext = SynchronizationContext.Current ?? new SynchronizationContext();
            _updateProgressAction = updateProgressAction;
        }

        public void Report(T value)
        {
            lock (_lock)
            {
                _latestValue = value;

                var now = DateTime.UtcNow;
                if (now - _lastUpdate >= _throttleInterval)
                {
                    _lastUpdate = now;
                    _synchronizationContext.Post(_ => _updateProgressAction(value), null);

                    // Cancel any pending flush
                    _flushTimer?.Dispose();
                    _flushTimer = null;
                }
                else
                {
                    // Schedule a flush for the latest value
                    _flushTimer?.Dispose();
                    var delay = _throttleInterval - (now - _lastUpdate);
                    _flushTimer = new Timer(_ => Flush(), null, delay, Timeout.InfiniteTimeSpan);
                }
            }
        }

        private void Flush()
        {
            lock (_lock)
            {
                if (_latestValue != null)
                {
                    _lastUpdate = DateTime.UtcNow;
                    var value = _latestValue;
                    _synchronizationContext.Post(_ => _updateProgressAction(value), null);
                }
            }
        }

        public void Dispose()
        {
            _flushTimer?.Dispose();
            Flush(); // Ensure last value is reported
        }
    }
}
