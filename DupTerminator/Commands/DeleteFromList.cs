using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DupTerminator.Commands
{
    class DeleteFromListCommand : ICommand
    {
        private ListViewSave _listDuplicates;
        private ListViewSave _backupListDuplicates;
        private int _index;

        public DeleteFromListCommand(ListViewSave listDuplicates, int index)
        {
            _listDuplicates = listDuplicates;
            _backupListDuplicates = listDuplicates.Clone();
            _index = index;
        }

        #region Члены ICommand

        public bool Execute()
        {
            return _listDuplicates.DeleteGroupFromList(_index);
        }

        public void UnExecute(ref ListViewSave listDuplicaste)
        {
            //throw new NotImplementedException();
            listDuplicaste = _backupListDuplicates;
            listDuplicaste.ColoringOfGroups();
        }

        #endregion
    }
}
