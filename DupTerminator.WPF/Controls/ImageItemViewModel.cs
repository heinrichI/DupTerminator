using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;

namespace DupTerminator.WPF.Controls
{
    public class ImageItemViewModel : PropertyChangedBase, IDisposable
    {
        private readonly IThumbnailProvider _thumbnailProvider;
        private BitmapImage? _thumbnail;
        private bool _isLoading;

        // A shared, frozen placeholder shown while the real thumbnail is loading.
        private static readonly BitmapImage _placeholder = CreatePlaceholder();

        public ImageItemViewModel(PHashFileInfoSearchItem searchItem, IThumbnailProvider thumbnailProvider)
        {
            SearchItem = searchItem;
            _thumbnailProvider = thumbnailProvider;

            RenameCommand = new RelayCommand((_) => OnRename(), (_) => CanRename());
            ViewFullSizeCommand = new RelayCommand((_) => OnViewFullSize());
        }

        public ExtendedFileInfo FileInfo => SearchItem.FileItem.FileInfo;
        public string FilePath => SearchItem.FileItem.FileInfo.Path;
        public string FileName => Path.GetFileName(SearchItem.FileItem.FileInfo.Name);

        public int HammingDistance => SearchItem.HammingDistance;

        public string Query
        {
            get
            {
                if (SearchItem.Type == PHashFileInfoSearchItem.SearchType.Seed)
                    return "seed";
                else if (SearchItem.Type == PHashFileInfoSearchItem.SearchType.Query)
                    return $"distance={SearchItem.HammingDistance.ToString()}";
                return "unknown";
            }
        }

        public string Dimensions => $"{SearchItem.FileItem.Width}x{SearchItem.FileItem.Height}";

        public ulong Size => SearchItem.FileItem.FileInfo.Size;

        public BitmapImage? Thumbnail
        {
            get
            {
                if (_thumbnail != null)
                    return _thumbnail;

                if (String.IsNullOrEmpty(FilePath))
                    return null;

                // Request the thumbnail to be loaded on the background thread.
                _thumbnailProvider.Enqueue(this);

                return _placeholder;
            }
            private set
            {
                _thumbnail = value;
                RaisePropertyChangedEvent();
            }
        }

        /// <summary>
        /// Called by the ThumbnailProvider on the UI thread once the thumbnail is ready.
        /// </summary>
        public void SetThumbnail(BitmapImage image)
        {
            if (image == null)
                return;

            Thumbnail = image;
        }

        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                RaisePropertyChangedEvent();
            }
        }

        public ICommand RenameCommand { get; }
        public ICommand ViewFullSizeCommand { get; }
        public PHashFileInfoSearchItem SearchItem { get; }

        private bool CanRename() => File.Exists(FilePath); //!_fileInfo.IsArchive && 

        private void OnRename()
        {
            // Implement rename logic
        }

        private void OnViewFullSize()
        {
            System.Diagnostics.Debug.WriteLine("ViewFullSizeCommand executed!");
        }

        private static BitmapImage CreatePlaceholder()
        {
            // Generate a small gray placeholder in code – no resource file needed.
            const int size = 200;
            var visual = new DrawingVisual();
            using (var dc = visual.RenderOpen())
            {
                dc.DrawRectangle(Brushes.LightGray, null, new System.Windows.Rect(0, 0, size, size));
            }

            var bitmap = new RenderTargetBitmap(size, size, 96, 96, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            bitmap.Freeze();

            var encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using var stream = new MemoryStream();
            encoder.Save(stream);
            stream.Position = 0;

            var image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = stream;
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }

        public void Dispose()
        {
            _thumbnail?.Freeze();
            _thumbnail = null;
        }
    }
}