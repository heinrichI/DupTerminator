using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Threading;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;
using DupTerminator.WPF.Controls;
using DupTerminator.WPF.Model;
using DupTerminator.WPF.Service;
using DupTerminator.WPF.View;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using static System.Formats.Asn1.AsnWriter;

namespace DupTerminator.WPF.ViewModel
{
    internal class MainViewModel : PropertyChangedBase
    {
        public SettingViewModel SettingViewModel { get; set; }

        public ImageGroupsViewModel ImageGroupsViewModel { get; }
        public ImageListViewModel ImageListViewModel { get; }
        public ClearDbCommand ClearDbCommand { get; }

        public MainViewModel(
            SettingViewModel settingViewModel,
            ImageGroupsViewModel imageGroupsViewModel,
            ImageListViewModel imageListViewModel,
            IImageProvider imageProvider,
            ClearDbCommand clearDbCommand,
            ILogger<MainViewModel> logger)
        {
            SettingViewModel = settingViewModel;
            ImageGroupsViewModel = imageGroupsViewModel;
            ImageListViewModel = imageListViewModel;
            _imageProvider = imageProvider;
            ClearDbCommand = clearDbCommand;
            _logger = logger;
            SettingViewModel.SearchCompleted += OnSearchCompleted;
        }

        internal void OnClosing(object? sender, CancelEventArgs e)
        {
            SettingViewModel.Save();
        }

        private void OnSearchCompleted(object sender, SearchCompletedEventArgs e)
        {
            SearchResults = e.Results;
            //SearchResults = new ObservableCollection<DuplicateGroup>(e.Results);
            //SearchResults.Clear();
            //if (e.Results is not null)
            //{
            //foreach (var item in e.Results)
            //{
            //SearchResults.Add(item);
            //}
            //RaisePropertyChangedEvent("SearchResults");
            //}

            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.Results is PHashSearchImageResult pHashSearchImageResult)
                    ImageListViewModel.UpdateList(pHashSearchImageResult.Images);
                else if (e.Results is PHashResult pResult)
                    ImageGroupsViewModel.UpdateGroups(pResult.Result);

            });

            SelectedTabPageIndex = 1;
        }


        private ResultBase _searchResults;
        public ResultBase SearchResults
        {
            get => _searchResults;
            set
            {
                _searchResults = value;
                RaisePropertyChangedEvent();
            }
        }
        //private ObservableCollection<ResultBase> _searchResults = new ObservableCollection<ResultBase>();
        //public ObservableCollection<ResultBase> SearchResults
        //{
        //    get => _searchResults;
        //    set
        //    {
        //        _searchResults = value;
        //        RaisePropertyChangedEvent();
        //    }
        //}

        int _selectedResultIndex;

        public int SelectedResultIndex
        {
            get { return _selectedResultIndex; }
            set
            {
                _selectedResultIndex = value;
                RaisePropertyChangedEvent();
            }
        }

        private readonly IImageProvider _imageProvider;
        private readonly ILogger<MainViewModel> _logger;
        private int _selectedTabPageIndex;
        public int SelectedTabPageIndex
        {
            get { return _selectedTabPageIndex; }
            set
            {
                _selectedTabPageIndex = value;
                RaisePropertyChangedEvent();
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
                        var image = _imageProvider.GetFullSizeFromArchive(asfi);
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
                        var image = _imageProvider.GetFullSizeFromArchive(afi);
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
                    else if (arg is ExtendedFileInfo efi)
                    {
                        if (System.IO.File.Exists(efi.Path))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = efi.Path,
                                UseShellExecute = true
                            });
                        }
                        else if (efi.Container is not null && System.IO.File.Exists(efi.Container.Path))
                        {
                            ArchiveFileInfo afi2 = new ArchiveFileInfo
                            {
                                ArchivePath = efi.Container.Path,
                                Path = efi.Path,
                                Name = efi.Name
                            };
                            var image = _imageProvider.GetFullSizeFromArchive(afi2);
                            if (image is not null)
                            {
                                var dialog = new ImageWindow(image, afi2.Size)
                                {
                                    Owner = System.Windows.Application.Current.MainWindow,
                                };

                                // Show dialog and wait for either worker completion or dialog close
                                dialog.Show();
                            }
                        }
                        else if (System.IO.Directory.Exists(efi.Container.Path))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = efi.Container.Path,
                                UseShellExecute = true
                            });
                        }
                    }
                }, arg => arg != null));
            }
        }


        ICommand _deleteFileCommand;
        public ICommand DeleteFileCommand
        {
            get
            {
                return _deleteFileCommand ?? (_deleteFileCommand = new RelayCommand(arg =>
                {
                    if (arg is not SimpleFileInfo fileInfo)
                        return;

                    if (SearchResults is not MD5ContainerResult result)
                        return;


                    var containersToRemove = result.Result
                        .Where(d => d.Key.First.Equals(fileInfo) || d.Key.Second.Equals(fileInfo)).ToArray();
                    if (containersToRemove.Any() && System.IO.File.Exists(fileInfo.Path))
                    {
                        var confirm = MessageBox.Show(
                            $"Удалить файл?\n\n{fileInfo.Path}",
                            "Подтверждение",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Warning);

                        if (confirm != MessageBoxResult.Yes)
                            return;

                        FileSystem.DeleteFile(
                                    fileInfo.Path,
                                    UIOption.AllDialogs,
                                    RecycleOption.SendToRecycleBin // Отправить в корзину
                                );
                        _logger.LogInformation($"Файл {fileInfo.Path} удален");
                        foreach (var containerToRemove in containersToRemove)
                        {
                            result.Result.Remove(containerToRemove);
                        }          
                    }
                }, arg =>
                {
                    if (arg is ExtendedFileInfo efi && efi.Container is DirectoryContainer)
                        return true;
                    return false;
                }));
            }
        }
    }

}
