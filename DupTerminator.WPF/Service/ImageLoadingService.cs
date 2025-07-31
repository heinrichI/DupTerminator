//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Media.Imaging;
//using DupTerminator.BusinessLogic.Abstraction;
//using DupTerminator.BusinessLogic.Model;
//using DupTerminator.WPF.Abstraction;
//using DupTerminator.WPF.Model;

//namespace DupTerminator.WPF.Service
//{
//    public class ImageLoadingService : IImageLoadingService
//    {
//        private readonly IArchiveService _archiveService;
//        private readonly IWindowsUtil _windowsUtil;
//        private readonly MemoryCache<BitmapImage> _thumbnailCache;
//        private readonly MemoryCache<BitmapImage> _fullSizeCache;
//        private readonly PriorityQueue<ImageLoadRequest> _loadQueue;

//        public ImageLoadingService(IArchiveService archiveService, IWindowsUtil windowsUtil)
//        {
//            _archiveService = archiveService;
//            _windowsUtil = windowsUtil;
//            _thumbnailCache = new MemoryCache<BitmapImage>(TimeSpan.FromMinutes(10), 100);
//            _fullSizeCache = new MemoryCache<BitmapImage>(TimeSpan.FromMinutes(5), 50);
//            _loadQueue = new PriorityQueue<ImageLoadRequest>();
//        }

//        public async Task<BitmapImage> LoadThumbnailAsync(ExtendedFileInfo fileInfo, int maxWidth, int maxHeight)
//        {
//            var cacheKey = $"{fileInfo.FullName}_{maxWidth}_{maxHeight}";

//            if (_thumbnailCache.TryGetValue(cacheKey, out var cachedImage))
//                return cachedImage;

//            var request = new ImageLoadRequest(fileInfo, maxWidth, maxHeight, isThumbnail: true);
//            _loadQueue.Enqueue(request, priority: 1); // Higher priority for visible items

//            return await request.TaskCompletionSource.Task;
//        }

//        public async Task<BitmapImage> LoadFullSizeAsync(ExtendedFileInfo fileInfo)
//        {
//            var cacheKey = fileInfo.FullName;

//            if (_fullSizeCache.TryGetValue(cacheKey, out var cachedImage))
//                return cachedImage;

//            var request = new ImageLoadRequest(fileInfo, 0, 0, isThumbnail: false);
//            _loadQueue.Enqueue(request, priority: 0); // Lower priority for full-size

//            return await request.TaskCompletionSource.Task;
//        }

//        private async Task ProcessLoadQueue()
//        {
//            while (true)
//            {
//                if (_loadQueue.TryDequeue(out var request, out _))
//                {
//                    try
//                    {
//                        BitmapImage image;

//                        if (fileInfo.IsArchive)
//                        {
//                            image = await LoadFromArchiveAsync(request);
//                        }
//                        else
//                        {
//                            image = await LoadFromFileSystemAsync(request);
//                        }

//                        if (request.IsThumbnail)
//                        {
//                            _thumbnailCache.Set(request.CacheKey, image);
//                        }
//                        else
//                        {
//                            _fullSizeCache.Set(request.CacheKey, image);
//                        }

//                        request.TaskCompletionSource.SetResult(image);
//                    }
//                    catch (Exception ex)
//                    {
//                        request.TaskCompletionSource.SetException(ex);
//                    }
//                }
//                await Task.Delay(100);
//            }
//        }

//        private async Task<BitmapImage> LoadFromArchiveAsync(ImageLoadRequest request)
//        {
//            // Implementation for loading from archive
//            using var stream = await _archiveService.ExtractFileAsync(request.FileInfo.FullName);
//            return await CreateBitmapImageAsync(stream, request.MaxWidth, request.MaxHeight);
//        }

//        private async Task<BitmapImage> LoadFromFileSystemAsync(ImageLoadRequest request)
//        {
//            using var stream = File.OpenRead(request.FileInfo.FullName);
//            return await CreateBitmapImageAsync(stream, request.MaxWidth, request.MaxHeight);
//        }

//        private async Task<BitmapImage> CreateBitmapImageAsync(Stream stream, int maxWidth, int maxHeight)
//        {
//            var bitmap = new BitmapImage();
//            bitmap.BeginInit();
//            bitmap.CacheOption = BitmapCacheOption.OnLoad;

//            if (maxWidth > 0 && maxHeight > 0)
//            {
//                bitmap.DecodePixelWidth = maxWidth;
//                bitmap.DecodePixelHeight = maxHeight;
//            }

//            bitmap.StreamSource = stream;
//            bitmap.EndInit();
//            bitmap.Freeze();

//            return bitmap;
//        }
//    }

//}
