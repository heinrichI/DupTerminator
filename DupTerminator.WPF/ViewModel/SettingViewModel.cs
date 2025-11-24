using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using DupTerminator.BusinessLogic;
using DupTerminator.BusinessLogic.Abstraction;
using DupTerminator.BusinessLogic.Model;
using DupTerminator.BusinessLogic.Model.Modes;
using DupTerminator.BusinessLogic.Service;
using DupTerminator.DataBase;
using DupTerminator.WindowsSpecific;
using DupTerminator.WPF.Commands;
using DupTerminator.WPF.Helper;
using DupTerminator.WPF.Model;
using DupTerminator.WPF.Service;
using Microsoft.Extensions.Logging;
using static System.Formats.Asn1.AsnWriter;

namespace DupTerminator.WPF.ViewModel
{
    internal class SettingViewModel : PropertyChangedBase, IDropable
    {
        private const string LOCATION_FILE_NAME = "locations.json";
        private const string SEARCH_SETTING_FILE_NAME = "searchSetting.json";
        private readonly IDBManager _dBManager;
        private readonly IPhashRepository _phashRepository;
        private readonly IWindowsUtil _windowsUtil;
        private readonly IArchiveService _archiveService;
        private readonly DbArchiveService _dbArchiveService;
        private readonly IPHashService _pHashService;
        private readonly IMIHFactory _mihFactory;
        private readonly ILogger<Searcher> _searchLogger;
        private readonly IProgressDialogService _progressDialogService;
        public event EventHandler<SearchCompletedEventArgs> SearchCompleted;

        public SettingViewModel(
            IDBManager dBManager,
            IPhashRepository phashRepository,
            IWindowsUtil windowsUtil,
            IArchiveService archiveService,
            DbArchiveService dbArchiveService,
            IPHashService pHashService,
            IMIHFactory mihFactory,
            ILogger<Searcher> searchLogger,
            IProgressDialogService progressDialogService)
        {
            var locations = SerializeHelper<SearchPathViewModel[]>.Load(LOCATION_FILE_NAME) ?? new SearchPathViewModel[0];
            SearchSetting = SerializeHelper<SearchSetting>.Load(SEARCH_SETTING_FILE_NAME) ?? new SearchSetting();

            _locationsObservable = new ObservableCollection<SearchPathViewModel>(locations);
            _dBManager = dBManager;
            _phashRepository = phashRepository;
            _windowsUtil = windowsUtil;
            _archiveService = archiveService;
            _dbArchiveService = dbArchiveService;
            _pHashService = pHashService;
            _mihFactory = mihFactory;
            _searchLogger = searchLogger;
            _progressDialogService = progressDialogService;
        }

        //private void OnSearchCompleted(ReadOnlyCollection<ResultBase> results)
        //{
        //    SearchCompleted?.Invoke(this, new SearchCompletedEventArgs(results));
        //}
        private void OnSearchCompleted(ResultBase results)
        {
            SearchCompleted?.Invoke(this, new SearchCompletedEventArgs(results));
        }

        public ObservableCollection<SettingsBase> Modes => new ObservableCollection<SettingsBase>(new List<SettingsBase>{ new MD5ModeSettings(), new DuplicateContainerSettings(), new PHashSettings() });


        private SettingsBase _selectedMode;
        public SettingsBase SelectedMode
        {
            get { return _selectedMode; }
            set
            {
                _selectedMode = value;
                this.RaisePropertyChangedEvent();
            }
        }


        ObservableCollection <SearchPathViewModel> _locationsObservable;
        public ObservableCollection<SearchPathViewModel> Locations
        {
            get { return _locationsObservable; }
        }

        public SearchSetting SearchSetting { get; set; }



        public bool UseDB
        {
            get { return SearchSetting.UseDB; }
            set
            {
                SearchSetting.UseDB = value;
                this.RaisePropertyChangedEvent();
            }
        }

        public ulong? SkipLessThan
        {
            get { return SearchSetting.SkipLessThan; }
            set
            {
                SearchSetting.SkipLessThan = value;
                this.RaisePropertyChangedEvent();
            }
        }

        #region IDropable Members

        void IDropable.Drop(object dropData)
        {
            var filepaths = dropData as string[];
            if (filepaths != null)
            {
                foreach (string path in filepaths)
                {
                    if (!String.IsNullOrEmpty(path))
                    {
                        if (IOHelper.IsDirectory(path))
                        {
                            _locationsObservable.Add(new SearchPathViewModel
                            {
                                Path = path,
                                IsDirectory = true,
                                SearchInSubfolder = true
                                //Image = IconReader.GetIcon(path, true);
                            });
                        }
                        else if (System.IO.File.Exists(path))
                        {
                            _locationsObservable.Add(new SearchPathViewModel
                            {
                                Path = path,
                                IsDirectory = false
                            });
                        }
                    }
                }
            }
        }

        internal void Save()
        {
            SerializeHelper<SearchPathViewModel[]>.Save(_locationsObservable.ToArray(), LOCATION_FILE_NAME);
            SerializeHelper<SearchSetting>.Save(SearchSetting, SEARCH_SETTING_FILE_NAME);
        }

        #endregion

        ICommand _startCommand;
        public ICommand StartCommand
        {
            get
            {
                //var locations = new ReadOnlyCollection<SearchPath>(
                //    Locations
                //    .Select(location => new SearchPath(location.Path, location.IsDirectory, location.SearchInSubFolder))
                //    .ToList());
                return _startCommand ?? (_startCommand = new StartCommand(
                    //locations,
                    //searchSetting,
                    this,
                    _dBManager,
                    _phashRepository,
                    _windowsUtil,
                    _archiveService,
                    _dbArchiveService,
                    _pHashService,
                    _mihFactory,
                    _searchLogger,
                    _progressDialogService,
                    OnSearchCompleted));
            }
        }
    }
}
