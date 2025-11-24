using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;

namespace DupTerminator.WPF.ViewModel
{
    public class ImageGroupsViewModel : PropertyChangedBase
    {
        private readonly ObservableCollection<ImageGroupViewModel> _imageGroups = new ObservableCollection<ImageGroupViewModel>();
        private readonly IImageProvider _imageLoadingService;

        public ReadOnlyObservableCollection<ImageGroupViewModel> ImageGroups { get; }

        public ImageGroupsViewModel(IImageProvider imageLoadingService)
        {
            _imageLoadingService = imageLoadingService;
            ImageGroups = new ReadOnlyObservableCollection<ImageGroupViewModel>(_imageGroups);

            ViewFullSizeCommand = new RelayCommand((_) => OnViewFullSize());
        }

        public void UpdateGroups(ReadOnlyCollection<PHashDuplicateGroup> duplicateGroups)
        {
            _imageGroups.Clear();

            foreach (var group in duplicateGroups)
            {
                _imageGroups.Add(new ImageGroupViewModel(group, _imageLoadingService));
            }
        }

        public ICommand ViewFullSizeCommand { get; }

        private void OnViewFullSize()
        {
            System.Diagnostics.Debug.WriteLine("ViewFullSizeCommand executed!");
        }

    }
}
