using System;
using System.Collections.Generic;
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

        public string Path { get; set; }

        public bool IsDirectory { get; set; }
    }
}
