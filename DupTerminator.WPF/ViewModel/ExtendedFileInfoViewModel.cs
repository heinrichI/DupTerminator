using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.WPF.ViewModel
{
    public class ExtendedFileInfoViewModel : PropertyChangedBase
    {
        private readonly ExtendedFileInfo _fileInfo;
        private bool _isSelected;
        private DuplicateGroupViewModel _group;

        public ExtendedFileInfoViewModel(ExtendedFileInfo fileInfo, DuplicateGroupViewModel group)
        {
            _fileInfo = fileInfo;
            _group = group;
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    this.RaisePropertyChangedEvent();
                    foreach (var item in _group.Files)
                    {
                        item.RaisePropertyChangedEvent(nameof(CanBeSelected));
                    }
                }
            }
        }

        public string Name => _fileInfo.Name;

        public string Path => _fileInfo.Path;
        public string CheckSum => _fileInfo.CheckSum;
        public ulong Size => _fileInfo.Size;
        public string Extension => _fileInfo.Extension;

        public bool CanBeSelected => IsSelected || _group.Files.Count - _group.Files.Count(f => f.IsSelected) > 1;
    }
}
