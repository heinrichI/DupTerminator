using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using DupTerminator.BusinessLogic.Model;

namespace DupTerminator.WPF.Model
{
    internal class ImageLoadRequest
    {
        public ExtendedFileInfo FileInfo { get; }
        public int MaxWidth { get; }
        public int MaxHeight { get; }
        public bool IsThumbnail { get; }
        public string CacheKey { get; }
        public TaskCompletionSource<BitmapImage> TaskCompletionSource { get; }

        public ImageLoadRequest(ExtendedFileInfo fileInfo, int maxWidth, int maxHeight, bool isThumbnail)
        {
            FileInfo = fileInfo;
            MaxWidth = maxWidth;
            MaxHeight = maxHeight;
            IsThumbnail = isThumbnail;
            CacheKey = isThumbnail ? $"{fileInfo.Path}_{maxWidth}_{maxHeight}" : fileInfo.Path;
            TaskCompletionSource = new TaskCompletionSource<BitmapImage>();
        }
    }
}
