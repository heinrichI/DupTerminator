using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;
using DupTerminator.WPF.Service;
using DupTerminator.WPF.View;
using DupTerminator.WPF.ViewModel;

namespace DupTerminator.WPF.Controls
{
    public class ImageListViewModel : PropertyChangedBase
    {
        private readonly IImageProvider _imageLoadingService;
        private readonly IProgressDialogService _progressDialogService;

        public ImageListViewModel(IImageProvider imageLoadingService, IProgressDialogService progressDialogService)
        {
            _imageLoadingService = imageLoadingService;
            _progressDialogService = progressDialogService;
        }

        public ObservableCollection<ImageItemViewModel> Images { get; } = new ObservableCollection<ImageItemViewModel>();

        internal void UpdateList(ReadOnlyCollection<PHashFileInfoSearchItem> images)
        {
            Images.Clear();
            foreach (var image in images)
            {
                Images.Add(new ImageItemViewModel(image, _imageLoadingService));
            }
        }

        ICommand _openFileCommand;
        public ICommand OpenFileCommand
        {
            get
            {
                return _openFileCommand ?? (_openFileCommand = new RelayCommand(arg =>
                {
                    if (arg is ArchiveSimpleFileInfo asfi)
                    {
                        var image = _imageLoadingService.GetFullSizeFromArchive(asfi);
                        if (image is not null)
                        {
                            var dialog = new ImageWindow(image, asfi.Size)
                            {
                                Owner = System.Windows.Application.Current.MainWindow,
                            };

                            // Show dialog and wait for either worker completion or dialog close
                            dialog.Show();
                        }
                    }
                    else if (arg is ArchiveFileInfo afi)
                    {
                        var image = _imageLoadingService.GetFullSizeFromArchive(afi);
                        if (image is not null)
                        {
                            var dialog = new ImageWindow(image, afi.Size)
                            {
                                Owner = System.Windows.Application.Current.MainWindow,
                            };

                            // Show dialog and wait for either worker completion or dialog close
                            dialog.Show();
                        }
                    }
                }, arg => arg != null));
            }
        }

        ICommand _openContainerCommand;
        public ICommand OpenContainerCommand
        {
            get
            {
                return _openContainerCommand ?? (_openContainerCommand = new RelayCommand(arg =>
                {
                    if (arg is ArchiveFileInfo afi)
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = afi.ArchivePath,
                            UseShellExecute = true
                        });
                    }
                }));
            }
        }

        ICommand _copyPathCommand;
        public ICommand CopyPathCommand
        {
            get
            {
                return _copyPathCommand ?? (_copyPathCommand = new RelayCommand(arg =>
                {
                    if (arg is ImageItemViewModel iivm)
                    {
                        Clipboard.SetText(iivm.FilePath);
                    }
                }));
            }
        }

        ICommand _searchByThisCommand;
        public ICommand SearchByThisCommand
        {
            get
            {
                return _searchByThisCommand ?? (_searchByThisCommand = new RelayCommand(async arg => 
                {
                    if (arg is ImageItemViewModel iivm)
                    {
                        await _progressDialogService.RunAsync(async (progress, cancelToken) =>
                        {
                            var result = await Searcher.ReSearchAsync(iivm.SearchItem.FileItem.Hash, progress, cancelToken);
                            if (result is not null)
                            {
                                UpdateResults(new PHashSearchImageResult(result));
                                System.Media.SystemSounds.Beep.Play();
                            }
                        });
                    }
                }));
            }
        }

        public SearcherPhashSearchImage Searcher { get; internal set; }
        public Action<ResultBase?> UpdateResults { get; internal set; }
    }
}
