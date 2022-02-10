using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DupTerminator.Commands
{
    class RenameLikeNeighbourCommand : ICommand
    {
        private ListViewSave _listDuplicates;
        private int _index;
        private string _oldName;

        public RenameLikeNeighbourCommand(ListViewSave listDuplicates, int index)
        {
            _listDuplicates = listDuplicates;
            _index = index;
            _oldName = _listDuplicates.GetFileName(_index);
        }


        #region Члены ICommand

        public bool Execute()
        {
            return _listDuplicates.RenameLikeNeighbour(_index);
        }

        public void UnExecute(ref ListViewSave listDuplicates)
        {
            _listDuplicates.RenameTo(_index, _oldName);
        }

        #endregion
    }
}
