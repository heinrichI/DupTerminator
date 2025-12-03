using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Automation;
using System.Windows.Media.Imaging;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Abstraction;

namespace DupTerminator.WPF.Service
{
    public sealed class ImageProvider : IImageProvider
    {
        private readonly int _thumbSize = 120;
        private readonly LruCache<string, BitmapImage> _thumbCache = new(500); // keep 500 thumbnails
        private readonly LruCache<string, BitmapImage> _fullCache = new(50);  // keep 50 full images
        private readonly IArchiveService _archiveService;

        public ImageProvider(IArchiveService archiveService)
        {
            _archiveService = archiveService;
        }

        public Task<BitmapImage?> GetThumbnailAsync(string fullPath)
        {
            if (_thumbCache.TryGet(fullPath, out var cached))
                return Task.FromResult(cached);

            // only start the load if the UI really displays it – *deferred load*:
            return Task.Run(async () =>
            {
                var bmp = await LoadThumbnailAsync(fullPath);
                _thumbCache.Add(fullPath, bmp);
                return bmp;
            });

            //return null; // placeholder for the UI
        }

        public BitmapImage? GetFullSizeAsync(string fullPath)
        {
            if (_fullCache.TryGet(fullPath, out var cached)) return cached;

            Task.Run(async () =>
            {
                var bmp = await LoadFullAsync(fullPath);
                _fullCache.Add(fullPath, bmp);
            });

            return null;
        }

        /// <summary>
        /// Load a thumbnail – supports archives by delegating
        /// to ArchiveService or just treats regular files.
        /// </summary>
        private async Task<BitmapImage> LoadThumbnailAsync(string fullPath)
        {
            // 1️⃣ Detect "archive:" protocol
            //if (fullPath.StartsWith("archive:"))
            //{
            //    // extract the inner path – you might have a custom format,
            //    // e.g. archive:/C:/zip/inner.jpg -> Uri might be
            //    //    new Uri("file://C:/zip"),
            //    //    inner = "inner.jpg"
            //    //   you will need your ArchiveService to get a Stream

            //    var (archive, innerFile) = ParseArchivePath(fullPath);
            //    var stream = await GetArchiveStreamAsync(archive, innerFile);
            //    return CreateZoomedBitmap(stream, _thumbSize);
            //}

            // regular file
            using var stream = File.OpenRead(fullPath);
            return CreateZoomedBitmap(stream, _thumbSize);
        }

        private async Task<BitmapImage> LoadFullAsync(string fullPath)
        {
            // similar to thumbnail but no resizing
            //if (fullPath.StartsWith("archive:"))
            //{
            //    var (archive, innerFile) = ParseArchivePath(fullPath);
            //    var stream2 = await GetArchiveStreamAsync(archive, innerFile);
            //    return CreateFullSizedBitmap(stream2);
            //}
            using var stream = File.OpenRead(fullPath);
            return CreateFullSizedBitmap(stream);
        }

        /* --- optional helper methods ----- */

        private BitmapImage CreateZoomedBitmap(Stream src, int size)
        {
            if (src == null)
                throw new ArgumentNullException(nameof(src));

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = src;
            bmp.DecodePixelWidth = size;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();

            //bmp.StreamSource = null; // Release the stream reference

            if (bmp.CanFreeze)
            {
                bmp.Freeze();
            }
            return bmp;
        }

        private BitmapImage CreateFullSizedBitmap(Stream src)
        {
            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.StreamSource = src;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.EndInit();
            bmp.Freeze();
            return bmp;
        }

        /* ----- archive helpers ----- */

        private (string archive, string innerFile) ParseArchivePath(string path)
        {
            // e.g. "archive:/C:/archives/foo.zip:inner.jpg"
            //  by convention we use a ':' after the archive path
            var withoutPrefix = path.Substring("archive:".Length);
            var parts = withoutPrefix.Split(':', 2);
            return (parts[0], parts[1]);
        }

        public async Task<BitmapImage?> GetThumbnailFromArchiveAsync(ArchiveFileInfo archiveFileInfo)
        {
            using var stream = _archiveService.GetStream(archiveFileInfo);

            //if (stream != null)
            //{
            //    // Copy the stream to a new MemoryStream within this method's scope.
            //    // This new stream will not be closed prematurely by the service layer's logic.
            //    using (MemoryStream localStream = new MemoryStream())
            //    {
            //        await stream.CopyToAsync(localStream);
            //        localStream.Position = 0; // Reset the position for the BitmapImage to read from the beginning.

            //        // The CreateZoomedBitmap method now receives this local stream.
            //        return CreateZoomedBitmap(localStream, _thumbSize);
            //    }
            //    // NOTE: The original 'stream' should be handled for disposal if necessary
            //    // within the _archiveService logic or after this method returns.
            //}

            return CreateZoomedBitmap(stream, _thumbSize);
            //return null;
        }

        //private async Task<Stream> GetArchiveStreamAsync(string archivePath, string innerFile)
        //{
        //    // 1️⃣ unwrap the protocol from your ArcService
        //    // 2️⃣ open the archive, find the entry, return a stream

        //    // here's a stub – fill with your real archive logic
        //    using var archive = ArchiveService.Open(archivePath); // returns e.g. ArchiveArchive
        //    var entry = archive.GetEntry(innerFile);
        //    return await Task.FromResult(entry.Open()); // returns a Stream
        //}
    }
}
