using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.BusinessLogic.Model
{
    public class SearchPath
    {
        public SearchPath(string path, bool isDirectory, bool searchInSubFolder)
        {
            Path = path;
            IsDirectory = isDirectory;
            SearchInSubFolder = searchInSubFolder;
        }

        public string Path { get; }
        public bool IsDirectory { get; }
        public bool SearchInSubFolder { get; }

        public string DriveLetter => Path.Substring(0, 2).ToLowerInvariant();
    }
}
