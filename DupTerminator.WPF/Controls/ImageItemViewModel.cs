using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;

namespace DupTerminator.WPF.Controls
{
    public class ImageItemViewModel : PropertyChangedBase, IDisposable
    {
        private readonly IImageProvider _imageLoadingService;
        private BitmapImage? _thumbnail;
        private bool _isLoading;

        public ImageItemViewModel(PHashFileInfoSearchItem searchItem, IImageProvider imageLoadingService)
        {
            SearchItem = searchItem;
            _imageLoadingService = imageLoadingService;

            RenameCommand = new RelayCommand((_) => OnRename(), (_) => CanRename());
            ViewFullSizeCommand = new RelayCommand((_) => OnViewFullSize());

            //LoadThumbnail();
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
                else
                {
                    IsLoading = true;
                    Debug.WriteLine($"Loading {FilePath}");
                    try
                    {
                        if (FileInfo is ArchiveFileInfo archiveFileInfo)
                            _thumbnail = _imageLoadingService.GetThumbnailFromArchive(archiveFileInfo);
                        else if (FileInfo is PdfFileInfo pdfFileInfo)
                            _thumbnail = _imageLoadingService.GetThumbnailFromPdf(pdfFileInfo);
                        else
                            _thumbnail = _imageLoadingService.GetThumbnailAsync(FilePath).Result;
                    }
                    finally
                    {
                        IsLoading = false;
                    }
                    return _thumbnail;
                }
            }
            private set
            {
                _thumbnail = value;
                RaisePropertyChangedEvent();
            }
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

        //private async void LoadThumbnail()
        //{
        //    if (_thumbnail != null) return;

        //    Debug.WriteLine("LoadThumbnail");
        //    IsLoading = true;
        //    try
        //    {
        //        if (FileInfo is ArchiveFileInfo archiveFileInfo)
        //            Thumbnail = await _imageLoadingService.GetThumbnailFromArchive(archiveFileInfo);
        //        else
        //            //Thumbnail = await _imageLoadingService.LoadThumbnailAsync(_fileInfo, 200, 200);
        //            Thumbnail = await _imageLoadingService.GetThumbnailAsync(FilePath);
        //    }
        //    finally
        //    {
        //        IsLoading = false;
        //    }
        //}

        private bool CanRename() => File.Exists(FilePath); //!_fileInfo.IsArchive && 

        private void OnRename()
        {
            // Implement rename logic
        }

        private void OnViewFullSize()
        {
            System.Diagnostics.Debug.WriteLine("ViewFullSizeCommand executed!");
        }

        public void Dispose()
        {
            _thumbnail?.Freeze();
            _thumbnail = null;
        }
    }
}
