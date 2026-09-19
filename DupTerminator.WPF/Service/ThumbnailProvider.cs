using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Controls;
using Microsoft.Extensions.Logging;

namespace DupTerminator.WPF.Service
{
    /// <summary>
    /// Loads thumbnails on a dedicated low-priority background thread and
    /// dispatches the results back to the UI thread. A bounded LRU cache keeps
    /// memory usage constant regardless of how many images are displayed.
    /// </summary>
    public sealed class ThumbnailProvider : IThumbnailProvider
    {
        private const int MAX_CACHED_THUMBNAILS = 500;

        private readonly IImageProvider _imageProvider;
        private readonly ILogger<ThumbnailProvider> _logger;
        private readonly Dispatcher _dispatcher;

        // LIFO queue – most recently requested items are loaded first (ideal for scrolling).
        private readonly Stack<ImageItemViewModel> _queue = new();
        private readonly AutoResetEvent _signal = new(false);
        private readonly LruCache<string, BitmapImage> _cache = new(MAX_CACHED_THUMBNAILS);

        private readonly Thread _loaderThread;
        private volatile bool _disposed;

        public ThumbnailProvider(IImageProvider imageProvider, ILogger<ThumbnailProvider> logger)
        {
            _imageProvider = imageProvider;
            _logger = logger;
            _dispatcher = Application.Current.Dispatcher;

            _loaderThread = new Thread(ThumbnailLoader)
            {
                Name = "ThumbnailLoader",
                IsBackground = true,
                Priority = ThreadPriority.Lowest
            };
            _loaderThread.Start();
        }

        public void Enqueue(ImageItemViewModel item)
        {
            if (item == null || _disposed)
                return;

            lock (_queue)
            {
                _queue.Push(item);
            }
            _signal.Set();
        }

        private void ThumbnailLoader()
        {
            while (true)
            {
                ImageItemViewModel? task = null;

                lock (_queue)
                {
                    if (_queue.Count > 0)
                    {
                        task = _queue.Pop();
                        if (task == null)
                            return; // shutdown signal
                    }
                }

                if (task != null)
                {
                    LoadAndDispatch(task);
                }
                else
                {
                    _signal.WaitOne(); // no more work – wait for a signal
                }
            }
        }

        private void LoadAndDispatch(ImageItemViewModel task)
        {
            try
            {
                var path = task.FilePath;
                if (string.IsNullOrEmpty(path))
                    return;

                if (_cache.TryGet(path, out var cached) && cached != null)
                {
                    DispatchResult(task, cached);
                    return;
                }

                BitmapImage? image = null;
                if (task.FileInfo is ArchiveFileInfo archiveFileInfo)
                {
                    image = _imageProvider.GetThumbnailFromArchive(archiveFileInfo);
                }
                else if (task.FileInfo is PdfFileInfo pdfFileInfo)
                {
                    image = _imageProvider.GetThumbnailFromPdf(pdfFileInfo);
                }
                else
                {
                    image = _imageProvider.GetThumbnailAsync(path).GetAwaiter().GetResult();
                }

                if (image != null)
                {
                    _cache.Add(path, image);
                    DispatchResult(task, image);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load thumbnail for {Path}", task.FilePath);
            }
        }

        private void DispatchResult(ImageItemViewModel task, BitmapImage image)
        {
            if (_disposed)
                return;

            _dispatcher.Invoke(DispatcherPriority.Normal, (Action)(() =>
            {
                if (!_disposed)
                    task.SetThumbnail(image);
            }));
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            // Signal the consumer to shut down.
            lock (_queue)
            {
                _queue.Push(null!);
            }
            _signal.Set();

            if (_loaderThread.IsAlive)
                _loaderThread.Join(TimeSpan.FromSeconds(2));

            _signal.Dispose();
        }
    }
}