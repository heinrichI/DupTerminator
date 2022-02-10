using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DupTerminator.ObjectModel;

namespace DupTerminator.Views
{
    public interface IMainView
    {
        event EventHandler<AddFolderEventArgs> AddFolderEvent;
        void Show();
        void AddToSearchFolders(DuplicateDirectory directory);
    }
}
