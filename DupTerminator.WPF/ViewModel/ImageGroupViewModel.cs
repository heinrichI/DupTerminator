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

        public DuplicateGroup DuplicateGroup { get; }
        public ObservableCollection<ImageItemViewModel> Images { get; } = new ObservableCollection<ImageItemViewModel>();

        public ICommand ViewFullSizeCommand { get; }


        public ImageGroupViewModel(DuplicateGroup duplicateGroup, IImageProvider imageLoadingService)
        {
            DuplicateGroup = duplicateGroup;
            _imageLoadingService = imageLoadingService;
            InitializeImages();

            ViewFullSizeCommand = new RelayCommand((_) => OnViewFullSize());
        }

        private void InitializeImages()
        {
            foreach (var file in DuplicateGroup.Files)
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
        private readonly ExtendedFileInfo _fileInfo;
        private readonly IImageProvider _imageLoadingService;
        private BitmapImage? _thumbnail;
        private bool _isLoading;

        public ExtendedFileInfo FileInfo => _fileInfo;
        public string FilePath => _fileInfo.Path;
        public string FileName => Path.GetFileName(_fileInfo.Name);

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

        public ImageItemViewModel(ExtendedFileInfo fileInfo, IImageProvider imageLoadingService)
        {
            _fileInfo = fileInfo;
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
                //Thumbnail = await _imageLoadingService.LoadThumbnailAsync(_fileInfo, 200, 200);
                Thumbnail = await _imageLoadingService.GetThumbnailAsync(_fileInfo.Path);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool CanRename() => File.Exists(_fileInfo.Path); //!_fileInfo.IsArchive && 

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
