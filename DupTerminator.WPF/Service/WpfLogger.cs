using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.WPF.Model;
using DupTerminator.WPF.ViewModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DupTerminator.WPF.Service
{
    public class WpfLogger : ILogger
    {
        private readonly ProgressDialogViewModel _viewModel;
        private readonly string _category;

        public WpfLogger(ProgressDialogViewModel viewModel, string category)
        {
            _viewModel = viewModel;
            _category = category;
        }

        public IDisposable BeginScope<TState>(TState state) => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                               Exception exception, Func<TState, Exception, string> formatter)
        {
            var message = formatter(state, exception);
            var logLevelString = logLevel.ToString();

            _viewModel.AddLogEntry(new LogEntry(DateTime.Now, logLevelString, message, _category));

            if (exception != null)
            {
                _viewModel.AddLogEntry(new LogEntry(DateTime.Now, "ERROR", exception.ToString(), _category));
            }
        }
    }
}
