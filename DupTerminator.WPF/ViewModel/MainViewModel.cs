using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.Diagnostics;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Threading;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.WPF.Abstraction;
using DupTerminator.WPF.Commands;
using DupTerminator.WPF.Controls;
using DupTerminator.WPF.Model;
using DupTerminator.WPF.Service;
using DupTerminator.WPF.View;
using Microsoft.Extensions.Logging;
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
            ClearDbCommand clearDbCommand)
        {
            SettingViewModel = settingViewModel;
            ImageGroupsViewModel = imageGroupsViewModel;
            ImageListViewModel = imageListViewModel;
            _imageProvider = imageProvider;
            ClearDbCommand = clearDbCommand;
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


        ICommand _openFileCommand;
        public ICommand OpenFileCommand
        {
            get
            {
                return _openFileCommand ?? (_openFileCommand = new RelayCommand(arg =>
                {
                    if (arg is ContainerEqInfo info)
                    {
                        if (System.IO.File.Exists(info.Path))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = info.Path,
                                UseShellExecute = true
                            });
                        }
                        else if(info.FileInfo is not null && info.FileInfo.Container is not null && System.IO.File.Exists(info.FileInfo.Container.Path))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = info.FileInfo.Container.Path,
                                UseShellExecute = true
                            });
                        }
                        else if (System.IO.Directory.Exists(info.Path))
                        {
                            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                            {
                                FileName = info.Path,
                                UseShellExecute = true
                            });
                        }
                    }
                    else if (arg is ArchiveSimpleFileInfo asfi)
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
                }, arg => arg != null));
            }
        }




        private int _selectedTabPageIndex;
        private readonly IImageProvider _imageProvider;

        public int SelectedTabPageIndex
        {
            get { return _selectedTabPageIndex; }
            set
            {
                _selectedTabPageIndex = value;
                RaisePropertyChangedEvent();
            }
        }
    }
}
