using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;

namespace DupTerminator.WPF.ViewModel
{
    public class ImageGroupViewModel : PropertyChangedBase
    {
        private readonly IImageProvider _imageLoadingService;

        public PHashDuplicateGroup DuplicateGroup { get; }
        public ObservableCollection<ImageItemViewModel> Images { get; } = new ObservableCollection<ImageItemViewModel>();

        public ICommand ViewFullSizeCommand { get; }


        public ImageGroupViewModel(PHashDuplicateGroup duplicateGroup, IImageProvider imageLoadingService)
        {
            DuplicateGroup = duplicateGroup;
            _imageLoadingService = imageLoadingService;
            InitializeImages();

            ViewFullSizeCommand = new RelayCommand((_) => OnViewFullSize());
        }

        private void InitializeImages()
        {
            foreach (PHashFileInfoSearchItem file in DuplicateGroup)
            {
                Images.Add(new ImageItemViewModel(file, _imageLoadingService));
            }
        }

        private void OnViewFullSize()
        {
            System.Diagnostics.Debug.WriteLine("ViewFullSizeCommand executed!");
        }
    }

    public class ImageItemViewModel : PropertyChangedBase, IDisposable
    {
        private readonly PHashFileInfoSearchItem _searchItem;
        private readonly IImageProvider _imageLoadingService;
        private BitmapImage? _thumbnail;
        private bool _isLoading;

        public ExtendedFileInfo FileInfo => _searchItem.FileItem;
        public string FilePath => _searchItem.FileItem.Path;
        public string FileName => Path.GetFileName(_searchItem.FileItem.Name);
        public string Query
        {
            get
            {
                if (_searchItem.Type == PHashFileInfoSearchItem.SearchType.Seed)
                    return "seed";
                else if(_searchItem.Type == PHashFileInfoSearchItem.SearchType.Query)
                    return $"distance={_searchItem.HammingDistance.ToString()}";
                return "unknown";
            }
        }

        public string Dimensions => "0x0";
        public string Size => "356Kb";

        public BitmapImage? Thumbnail
        {
            get => _thumbnail;
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

        public ImageItemViewModel(PHashFileInfoSearchItem searchItem, IImageProvider imageLoadingService)
        {
            _searchItem = searchItem;
            _imageLoadingService = imageLoadingService;

            RenameCommand = new RelayCommand((_) => OnRename(), (_) => CanRename());
            ViewFullSizeCommand = new RelayCommand((_) => OnViewFullSize());

            LoadThumbnail();
        }

        private async void LoadThumbnail()
        {
            if (_thumbnail != null) return;

            IsLoading = true;
            try
            {
                if (FileInfo is ArchiveFileInfo archiveFileInfo)
                    Thumbnail = await _imageLoadingService.GetThumbnailFromArchiveAsync(archiveFileInfo);
                else
                    //Thumbnail = await _imageLoadingService.LoadThumbnailAsync(_fileInfo, 200, 200);
                    Thumbnail = await _imageLoadingService.GetThumbnailAsync(FilePath);
            }
            finally
            {
                IsLoading = false;
            }
        }

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
