using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DupTerminator.Views;

namespace DupTerminator.Test
{
    public class TestMainView : IMainView
    {
        public event EventHandler<AddFolderEventArgs> AddFolderEvent;

        public void RiseAddFolderEvent(AddFolderEventArgs args)
        {
            if (AddFolderEvent != null)
                AddFolderEvent(this, args);
        }


        #region Члены IMainView


        public void Show()
        {
            throw new NotImplementedException();
        }


        public void AddToSearchFolders(DupTerminator.ObjectModel.DuplicateDirectory directory)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
