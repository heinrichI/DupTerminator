using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.WPF.Abstraction
{
    public interface IImageLoadingService
    {
        Task<BitmapImage> LoadThumbnailAsync(ExtendedFileInfo fileInfo, int maxWidth, int maxHeight);
        Task<BitmapImage> LoadFullSizeAsync(ExtendedFileInfo fileInfo);
    }
}
