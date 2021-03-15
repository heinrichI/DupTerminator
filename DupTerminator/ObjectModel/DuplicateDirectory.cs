using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DupTerminator.ObjectModel
{
    public class DuplicateDirectory
    {
        //private string _path;
        public string Path { get; set; }
        //private bool _isSubDir;
        public bool IsSubDir { get; set; }
        //private TypeFolder _type;
        public TypeFolder Type { get; set; }
        //private bool _checked;
        public bool Checked { get; set; }

        /*public string Path
        {
            get { return _path; }
        }*/

        public DuplicateDirectory(string path, bool isSubDir, TypeFolder type, bool checkDir)
        {
            Path = path;
            IsSubDir = isSubDir;
            Type = type;
            Checked = checkDir;
        }

        public DuplicateDirectory(string path, TypeFolder type)
        {
            Path = path;
            Type = type;
        }


        public override bool Equals(object obj)
        {
            DuplicateDirectory comparedObject = obj as DuplicateDirectory;
            return comparedObject != null ? comparedObject.Path == this.Path : false;
        }
    }
}
