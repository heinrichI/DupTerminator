using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DupTerminator.WPF.ViewModel
{
    public class SearchPathViewModel : PropertyChangedBase
    {
        //BitmapSource _image;
        //public BitmapSource Image
        //{
        //    get { return _image; }
        //    set
        //    {
        //        _image = value;
        //        RaisePropertyChangedEvent();
        //    }
        //}

        public bool SearchInSubfolder { get; set; }

        private string _path;
        public string Path
        {
            get => _path;
            set
            {
                if (_path == value)
                    return;
                var cleanValue = value?.TrimEnd('\\', '/');
                _path = cleanValue;
                RaisePropertyChangedEvent();
                // Проверяем, является ли путь существующей директорией
                IsDirectory = !string.IsNullOrEmpty(cleanValue) && Directory.Exists(cleanValue);
            }
        }

        public bool IsDirectory { get; set; }
    }
}
