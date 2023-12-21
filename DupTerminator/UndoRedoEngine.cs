using DupTerminator.BusinessLogic;
using DupTerminator.Command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DupTerminator
{
    class UndoRedoEngine
    {
        public delegate void ActoinAppledHandler();
        public event ActoinAppledHandler OnActoinAppledEvent;

        //Stack<UndoRedoState> _UndoStack
        //private Stack<ListViewSave> _UndoStack;
        //private Stack<ListViewSave> _RedoStack;
        /*private Stack<Command> _UndoCommandStack;
        private Stack<Command> _RedoCommandStack;*/
        private Stack<ICommand> _undoCommandStack;
        private Stack<ICommand> _redoCommandStack;
        public ListViewSave ListDuplicates;

        public UndoRedoEngine()
        {
            //_UndoStack = new Stack<ListViewSave>();
            //_RedoStack = new Stack<ListViewSave>();
            _undoCommandStack = new Stack<ICommand>();
            _redoCommandStack = new Stack<ICommand>();
            ListDuplicates = new ListViewSave();
        }

        public bool Undo()
        {
            /*if (_UndoStack.Count > 0)
            {
                ListDuplicates = _UndoStack.Pop();
                OnActoinAppledEvent();
                return true;
            }
            return false;*/
            if (_undoCommandStack.Count > 0)
            {
                ICommand command = _undoCommandStack.Pop();
                command.UnExecute(ref ListDuplicates);
                //ListDuplicates = command.
                _redoCommandStack.Push(command);
                OnActoinAppledEvent();
                return true;
            }
            return false;
        }

        public bool Redo()
        {
            if (_redoCommandStack.Count > 0)
            {
                ICommand command = _redoCommandStack.Pop();
                command.Execute();
                _undoCommandStack.Push(command);
                OnActoinAppledEvent();
                return true;
            }
            return false;
        }

        public bool UndoEnable()
        {
            //return (_UndoStack.Count > 0);
            return (_undoCommandStack.Count > 0);
        }

        public bool RedoEnable()
        {
            return (_redoCommandStack.Count > 0);
        }

        public void DeleteGroupsFromList(int index)
        {
            //_UndoStack.Push(ListDuplicates.Clone());
            //ListDuplicates.DeleteGroupFromList(indexOfGroupWithAllChecked);
            //Command cmd = new DeleteFromListCommand(ListDuplicates, indexOfGroupWithAllChecked);
            ICommand cmd = new DeleteFromListCommand(ListDuplicates, index);
            _undoCommandStack.Push(cmd);
            cmd.Execute();
            OnActoinAppledEvent();
        }

        public void RenameLikeNeighbour(int index)
        {
            //ListDuplicates.RenameLikeNeighbour(indexOfGroupWithAllChecked);
            //_UndoStack.Push(ListDuplicates.Clone());
            ICommand cmd = new RenameLikeNeighbourCommand(ListDuplicates, index);
            _undoCommandStack.Push(cmd);
            _redoCommandStack.Clear();
            cmd.Execute();
            OnActoinAppledEvent();
        }

        public bool RenameTo(int index, string name)
        {
            ICommand cmd = new RenameToCommand(ListDuplicates, index, name);
            if (cmd.Execute())
            {
                _undoCommandStack.Push(cmd);
                _redoCommandStack.Clear();
                OnActoinAppledEvent();
                return true;
            }
            return false;
        }
    }
}
