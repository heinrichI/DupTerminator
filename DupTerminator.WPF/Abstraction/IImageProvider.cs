using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.WPF.Abstraction
{
    public interface IImageProvider
    {
        Task<BitmapImage?> GetThumbnailAsync(string fullPath);

        BitmapImage? GetFullSizeAsync(string fullPath);
        BitmapImage? GetThumbnailFromArchive(ArchiveFileInfo archiveFileInfo);
        BitmapImage? GetThumbnailFromPdf(PdfFileInfo pdfFileInfo);
    }
}
