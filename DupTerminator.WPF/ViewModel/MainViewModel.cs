using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Configuration;
using System.DirectoryServices;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Input;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.WPF.Commands;
using DupTerminator.WPF.Model;
using DupTerminator.WPF.Service;
using Microsoft.Extensions.Logging;
using static System.Formats.Asn1.AsnWriter;

namespace DupTerminator.WPF.ViewModel
{
    internal class MainViewModel : PropertyChangedBase
    {
        public SettingViewModel SettingViewModel { get; set; }

        public ImageGroupsViewModel ImageGroupsViewModel { get; set; }

        public MainViewModel(
            SettingViewModel settingViewModel,
            ImageGroupsViewModel imageGroupsViewModel)
        {
            SettingViewModel = settingViewModel;
            ImageGroupsViewModel = imageGroupsViewModel;
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

            if (e.Results is PHashResult pResult)
                ImageGroupsViewModel.UpdateGroups(pResult.Result);
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
                    if (arg is string filePath && !string.IsNullOrEmpty(filePath) && System.IO.File.Exists(filePath))
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
                        {
                            FileName = filePath,
                            UseShellExecute = true
                        });
                    }
                }, arg => arg != null));
            }
        }
    }
}
