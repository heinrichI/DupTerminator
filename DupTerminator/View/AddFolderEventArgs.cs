using DupTerminator.BusinessLogic.ObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace DupTerminator.View
{
    public class AddFolderEventArgs : EventArgs
    {
        /*private string _path;
        private bool _isSubDir;
        private TypeFolder _type;

        public AddFolderEventArgs(string path, bool isSubDir, TypeFolder type)
        {
            _path = path;
            _isSubDir = isSubDir;
            _type = type;
        }

        public AddFolderEventArgs(string path, TypeFolder type)
        {
            _path = path;
            _type = type;
        }

        public string Path
        {
            get { return _path; }
        }

        public bool IsSubDir
        {
            get { return _isSubDir; }
        }

        public TypeFolder Type
        {
            get { return _type; }
        }*/

        private DuplicateDirectory _directory;

        public AddFolderEventArgs(DuplicateDirectory directory)
        {
            _directory = directory;
        }

        public DuplicateDirectory Directory
        {
            get { return _directory; }
        }
    }
}
