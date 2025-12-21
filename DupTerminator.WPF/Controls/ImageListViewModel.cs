using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;
using DupTerminator.WPF.ViewModel;

namespace DupTerminator.WPF.Controls
{
    public class ImageListViewModel : PropertyChangedBase
    {
        private readonly IImageProvider _imageLoadingService;

        public ImageListViewModel(IImageProvider imageLoadingService)
        {
            _imageLoadingService = imageLoadingService;
        }

        public ObservableCollection<ImageItemViewModel> Images { get; } = new ObservableCollection<ImageItemViewModel>();

        internal void UpdateList(ReadOnlyCollection<PHashFileInfoSearchItem> images)
        {
            foreach (var image in images)
            {
                Images.Add(new ImageItemViewModel(image, _imageLoadingService));
            }
        }
    }
}
