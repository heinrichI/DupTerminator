using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace DupTerminator.WPF.Abstraction
{
    public interface IImageProvider
    {
        Task<BitmapImage?> GetThumbnailAsync(string fullPath);

        BitmapImage? GetFullSizeAsync(string fullPath);
    }
}
