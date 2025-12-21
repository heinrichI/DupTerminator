using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using DupTerminator.WPF.Controls;

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
}
