using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DupTerminator.Command
{
    //public abstract class Command
    interface ICommand
    {
        /*public ListViewSave ListDuplicates;
        public abstract void Execute();
        public abstract void UnExecute();*/
        bool Execute();
        void UnExecute(ref ListViewSave listDuplicates);
    }
}
