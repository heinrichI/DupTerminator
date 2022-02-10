using DupTerminator.BusinessLogic.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.View
{
    public interface IMainView
    {
        event EventHandler<AddFolderEventArgs> AddFolderEvent;

        event EventHandler AboutClick;

        void Show();

        void AddToSearchFolders(DuplicateDirectory directory);
    }
}
