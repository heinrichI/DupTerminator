using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DupTerminator.Command
{
    class RenameToCommand : ICommand
    {
        private ListViewSave _listDuplicates;
        private int _index;
        private string _oldName;
        private string _newName;

        public RenameToCommand(ListViewSave listDuplicates, int index, string name)
        {
            _listDuplicates = listDuplicates;
            _index = index;
            _oldName = _listDuplicates.GetFileName(_index);
            _newName = name;
        }

        #region Члены ICommand

        public bool Execute()
        {
            return _listDuplicates.RenameTo(_index, _newName);
        }

        public void UnExecute(ref ListViewSave listDuplicates)
        {
            _listDuplicates.RenameTo(_index, _oldName);
        }

        #endregion
    }
}
