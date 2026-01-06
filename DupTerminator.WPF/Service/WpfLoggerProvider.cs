using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.WPF.ViewModel;
using Microsoft.Extensions.Logging;

namespace DupTerminator.WPF.Service
{
    public class WpfLoggerProvider : ILoggerProvider
    {
        private readonly ProgressDialogViewModel _viewModel;

        public WpfLoggerProvider(ProgressDialogViewModel viewModel)
        {
            _viewModel = viewModel;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new WpfLogger(_viewModel, categoryName);
        }

        public void Dispose()
        {
            // Cleanup if needed
        }
    }
}
