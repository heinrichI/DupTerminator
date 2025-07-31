using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.WPF.Service
{
    /// <summary>
    /// Abstraction: the view-model does **not** know *how* the dialog
    /// is displayed (modal, modeless, embedded, etc.)
    /// </summary>
    public interface IProgressDialogService
    {
        Task RunAsync(Func<IProgress<ProgressDto>, CancellationToken, Task> worker);
    }

}
