using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;

namespace DupTerminator.WPF.ViewModel
{
    public class ImageGroupsViewModel : PropertyChangedBase
    {
        private readonly ObservableCollection<ImageGroupViewModel> _imageGroups = new ObservableCollection<ImageGroupViewModel>();
        private readonly IThumbnailProvider _thumbnailProvider;

        public ReadOnlyObservableCollection<ImageGroupViewModel> ImageGroups { get; }

        public ImageGroupsViewModel(IThumbnailProvider thumbnailProvider)
        {
            _thumbnailProvider = thumbnailProvider;
            ImageGroups = new ReadOnlyObservableCollection<ImageGroupViewModel>(_imageGroups);

            ViewFullSizeCommand = new RelayCommand((_) => OnViewFullSize());
        }

        public void UpdateGroups(ReadOnlyCollection<PHashDuplicateGroup> duplicateGroups)
        {
            //Dispatcher.CurrentDispatcher.Invoke(() =>
            //{
                _imageGroups.Clear();

                foreach (var group in duplicateGroups)
                {
                    _imageGroups.Add(new ImageGroupViewModel(group, _thumbnailProvider));
                }
            //});
        }

        public ICommand ViewFullSizeCommand { get; }

        private void OnViewFullSize()
        {
            System.Diagnostics.Debug.WriteLine("ViewFullSizeCommand executed!");
        }

    }
}
