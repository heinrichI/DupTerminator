using DupTerminator.BusinessLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DupTerminator.View
{
    public class FileViewModel : BasePropertyChanged
    {
        public FileViewModel(ExtendedFileInfo file)
        {
            Path = file.fileInfo.FullName;
            Size = file.fileInfo.Length;
        }

        public FileViewModel(string path, long size)
        {
            Path = path;
            Size = size;
        }

        bool _delete;
        public bool Delete
        {
            get { return _delete; }
            set
            {
                _delete = value;
                RaisePropertyChangedEvent();
            }
        }

        public string Path { get; }
        public long Size { get; }
        public string Checksum { get; internal set; }
    }
}
