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
            Path = file.FullName;
            Size = file.Size;
        }

        public FileViewModel(string path, ulong size)
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
        public ulong Size { get; }
        public string Checksum { get; internal set; }
    }
}
