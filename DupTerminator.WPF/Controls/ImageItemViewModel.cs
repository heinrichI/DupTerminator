using System;
using System.Collections.Generic;
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
        private readonly PHashFileInfoSearchItem _searchItem;
        private readonly IImageProvider _imageLoadingService;
        private BitmapImage? _thumbnail;
        private bool _isLoading;

        public ImageItemViewModel(PHashFileInfoSearchItem searchItem, IImageProvider imageLoadingService)
        {
            _searchItem = searchItem;
            _imageLoadingService = imageLoadingService;

            RenameCommand = new RelayCommand((_) => OnRename(), (_) => CanRename());
            ViewFullSizeCommand = new RelayCommand((_) => OnViewFullSize());

            //LoadThumbnail();
        }

        public ExtendedFileInfo FileInfo => _searchItem.FileItem.FileInfo;
        public string FilePath => _searchItem.FileItem.FileInfo.Path;
        public string FileName => Path.GetFileName(_searchItem.FileItem.FileInfo.Name);

        public int HammingDistance => _searchItem.HammingDistance;

        public string Query
        {
            get
            {
                if (_searchItem.Type == PHashFileInfoSearchItem.SearchType.Seed)
                    return "seed";
                else if (_searchItem.Type == PHashFileInfoSearchItem.SearchType.Query)
                    return $"distance={_searchItem.HammingDistance.ToString()}";
                return "unknown";
            }
        }

        public string Dimensions => $"{_searchItem.FileItem.Width}x{_searchItem.FileItem.Height}";

        public ulong Size => _searchItem.FileItem.FileInfo.Size;

        public BitmapImage? Thumbnail
        {
            get
            {
                if (_thumbnail != null)
                    return _thumbnail;
                else
                {
                    IsLoading = true;
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
