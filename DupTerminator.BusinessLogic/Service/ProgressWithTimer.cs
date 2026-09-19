//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

//namespace DupTerminator.BusinessLogic.Service
//{
//    public class ProgressWithTimer<T> : IProgress<T> where T : class, IEquatable<T>
//    {
//        private readonly Dictionary<string, T> _latestProgress = new(); // Track latest per drive
//        private readonly Action<T> _updateAction;
//        private readonly System.Windows.Threading.DispatcherTimer _timer;
//        private readonly object _lock = new();

//        public ProgressWithTimer(TimeSpan interval, Action<T> updateAction)
//        {
//            _updateAction = updateAction;
//            _timer = new System.Windows.Threading.DispatcherTimer(DispatcherPriority.Background)
//            {
//                Interval = interval
//            };
//            _timer.Tick += Timer_Tick;
//            _timer.Start();
//        }

//        private void Timer_Tick(object sender, EventArgs e)
//        {
//            lock (_lock)
//            {
//                foreach (var kvp in _latestProgress)
//                {
//                    _updateAction(kvp.Value); // Always update UI with latest per drive
//                }
//                // Optional: Clear after update? Not needed - we always want latest
//            }
//        }

//        public void Report(T value)
//        {
//            if (value is not ProgressDto dto) return;

//            lock (_lock)
//            {
//                _latestProgress[dto.PhisicalDrive] = value; // Always store latest
//            }
//        }

//        public void Dispose() => _timer.Stop();
//    }
//}
